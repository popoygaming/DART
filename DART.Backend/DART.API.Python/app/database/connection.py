from collections.abc import Generator
from functools import lru_cache

from sqlalchemy import create_engine
from sqlalchemy.engine import Engine
from sqlalchemy.orm import Session, declarative_base, sessionmaker

from app.core.config import get_settings


Base = declarative_base()


def _engine_connect_args(database_url: str) -> dict[str, bool]:
    if database_url.startswith("sqlite"):
        return {"check_same_thread": False}
    return {}


@lru_cache
def get_engine() -> Engine:
    settings = get_settings()
    return create_engine(
        settings.DATABASE_URL,
        pool_pre_ping=True,
        connect_args=_engine_connect_args(settings.DATABASE_URL),
    )


@lru_cache
def get_session_factory() -> sessionmaker[Session]:
    return sessionmaker(
        autocommit=False,
        autoflush=False,
        bind=get_engine(),
        expire_on_commit=False,
    )


def get_db() -> Generator[Session, None, None]:
    db = get_session_factory()()
    try:
        yield db
    finally:
        db.close()


def clear_database_state() -> None:
    try:
        engine = get_engine()
        engine.dispose()
    except Exception:
        pass
    get_session_factory.cache_clear()
    get_engine.cache_clear()
