from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from langgraph.types import Command
from .contracts import CoordinatorRequest, HumanDecision
from .graph import build_graph, postgres_checkpointer
from .settings import Settings

settings = Settings()


@asynccontextmanager
async def lifespan(app: FastAPI):
    settings.validate_runtime()
    async with postgres_checkpointer(settings) as checkpointer:
        app.state.graph = build_graph(checkpointer)
        yield


app = FastAPI(title="ExpenseGuard Coordinator", lifespan=lifespan)


@app.get("/health")
async def health() -> dict[str, str | bool]:
    return {"status": "healthy", "fake_mode": settings.fake_mode}


@app.post("/workflows")
async def start(request: CoordinatorRequest) -> dict:
    config = {"configurable": {"thread_id": request.workflow_id}}
    try:
        return await app.state.graph.ainvoke(request.model_dump(), config=config)
    except Exception as exc:
        raise HTTPException(503, detail={"code": "WORKFLOW_SAFE_FAILURE", "message": type(exc).__name__}) from exc


@app.post("/workflows/{workflow_id}/resume")
async def resume(workflow_id: str, decision: HumanDecision) -> dict:
    config = {"configurable": {"thread_id": workflow_id}}
    try:
        return await app.state.graph.ainvoke(Command(resume=decision.model_dump()), config=config)
    except Exception as exc:
        raise HTTPException(503, detail={"code": "WORKFLOW_SAFE_FAILURE", "message": type(exc).__name__}) from exc
