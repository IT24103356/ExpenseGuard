FROM python:3.12-slim
WORKDIR /app
COPY . .
RUN if [ -f pyproject.toml ]; then \
      pip install --no-cache-dir .; \
    elif [ -f ai/expenseguard-agents/pyproject.toml ]; then \
      pip install --no-cache-dir ./ai/expenseguard-agents; \
    else \
      echo "expenseguard-agents project not found in the Docker build context" && ls -la && exit 1; \
    fi
EXPOSE 8088
CMD ["uvicorn", "expenseguard_agents.app:app", "--host", "0.0.0.0", "--port", "8088"]
