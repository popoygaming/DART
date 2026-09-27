from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.core.config import get_settings
from app.database.connection import get_engine


@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.settings = get_settings()
    app.state.engine = get_engine()
    yield


app = FastAPI(
    title="DART API Python",
    description="Backend API for DART (Document Archiving and Retrieval Tool)",
    version="0.1.0",
    lifespan=lifespan,
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "message": "DART API is running"}
