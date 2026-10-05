import json
import os
from datetime import date
from decimal import Decimal
from typing import Protocol

from pydantic import BaseModel, ConfigDict, Field, field_validator


class ReceiptExtraction(BaseModel):
    model_config = ConfigDict(extra="forbid")

    vendor: str | None = None
    amount: Decimal | None = Field(default=None, gt=0)
    purchase_date: date | None = None
    currency: str | None = Field(default=None, pattern=r"^[A-Z]{3}$")
    confidence: float = Field(ge=0, le=1)
    requires_manual_review: bool
    review_reasons: list[str] = Field(default_factory=list)

    @field_validator("currency", mode="before")
    @classmethod
    def normalize_currency(cls, value: str | None) -> str | None:
        return value.upper() if value else None


class ExtractionProvider(Protocol):
    def extract(self, receipt_text: str) -> ReceiptExtraction: ...


class ReceiptExtractionAgent:
    def __init__(self, provider: ExtractionProvider, review_threshold: float = 0.80):
        self.provider = provider
        self.review_threshold = review_threshold

    def run(self, receipt_text: str) -> ReceiptExtraction:
        if not receipt_text.strip():
            return ReceiptExtraction(
                confidence=0, requires_manual_review=True, review_reasons=["empty_receipt_text"]
            )
        result = self.provider.extract(receipt_text)
        reasons = list(result.review_reasons)
        if result.confidence < self.review_threshold:
            reasons.append("low_confidence")
        missing = [
            name
            for name in ("vendor", "amount", "purchase_date", "currency")
            if getattr(result, name) is None
        ]
        reasons.extend(f"missing_{name}" for name in missing)
        return result.model_copy(
            update={
                "requires_manual_review": bool(reasons),
                "review_reasons": sorted(set(reasons)),
            }
        )


class GeminiReceiptExtractor:
    """Production provider. Configuration is read only from environment variables."""

    def __init__(self) -> None:
        api_key = os.environ.get("GEMINI_API_KEY")
        if not api_key:
            raise RuntimeError("GEMINI_API_KEY is not configured")
        from google import genai

        self.client = genai.Client(api_key=api_key)
        self.model = os.environ.get("GEMINI_MODEL", "gemini-2.5-flash")

    def extract(self, receipt_text: str) -> ReceiptExtraction:
        prompt = (
            "Extract receipt fields as strict JSON with vendor, amount, purchase_date "
            "(YYYY-MM-DD), currency (ISO-4217), confidence (0..1), "
            "requires_manual_review, and review_reasons. Receipt text:\n" + receipt_text
        )
        response = self.client.models.generate_content(model=self.model, contents=prompt)
        raw = response.text.strip().removeprefix("```json").removesuffix("```").strip()
        return ReceiptExtraction.model_validate(json.loads(raw))
