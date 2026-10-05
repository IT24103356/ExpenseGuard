from decimal import Decimal
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid", str_strip_whitespace=True)


class PolicyAgentInput(StrictModel):
    claim_id: int = Field(gt=0)
    category: str = Field(min_length=1, max_length=100)
    amount: Decimal = Field(ge=0)
    currency: str = Field(pattern=r"^[A-Z]{3}$")
    authoritative_violations: list[str] = Field(max_length=50)
    context: str | None = Field(default=None, max_length=2000)


class PolicyRecommendation(StrictModel):
    recommendation: Literal["approve", "request_information", "escalate", "reject"]
    explanation: str = Field(min_length=1, max_length=1000)
    cited_rule_codes: list[str] = Field(max_length=50)
    confidence: float = Field(ge=0, le=1)
    advisory_only: Literal[True] = True


class FraudAgentInput(StrictModel):
    claim_id: int = Field(gt=0)
    risk_score: Decimal = Field(ge=0, le=100)
    deterministic_flags: list[str] = Field(max_length=50)
    evidence_summary: dict[str, str | int | float | bool | None] = Field(max_length=50)
    analyst_context: str | None = Field(default=None, max_length=2000)


class FraudRecommendation(StrictModel):
    recommendation: Literal["dismiss", "review", "escalate"]
    explanation: str = Field(min_length=1, max_length=1000)
    cited_flag_codes: list[str] = Field(max_length=50)
    confidence: float = Field(ge=0, le=1)
    advisory_only: Literal[True] = True
