from pathlib import Path

import pytest
from fastapi.testclient import TestClient
from sqlalchemy import text

from app.core.config import clear_settings_cache, get_settings
from app.database.connection import clear_database_state, get_db, get_engine
from app.main import app


def _configure_test_env(monkeypatch, tmp_path: Path) -> None:
    db_file = tmp_path / "test_dart_api.db"
    monkeypatch.setenv("DATABASE_URL", f"sqlite:///{db_file.as_posix()}")
    monkeypatch.setenv("JWT_SECRET", "test-secret")
    monkeypatch.setenv("JWT_ALGORITHM", "HS256")
    monkeypatch.setenv("ACCESS_TOKEN_EXPIRE_MINUTES", "60")
    monkeypatch.setenv("DOCUMENT_ROOT", str(tmp_path / "documents"))
    monkeypatch.setenv("OCR_SERVICE_URL", "http://localhost:9000")
    monkeypatch.setenv("OCR_SERVICE_TIMEOUT", "30")


def _reset_runtime_state() -> None:
    clear_database_state()
    clear_settings_cache()


def test_application_starts_with_database_config(monkeypatch, tmp_path: Path) -> None:
    _configure_test_env(monkeypatch, tmp_path)
    _reset_runtime_state()

    with TestClient(app) as client:
        response = client.get("/health")

    assert response.status_code == 200


def test_settings_can_be_loaded(monkeypatch, tmp_path: Path) -> None:
    _configure_test_env(monkeypatch, tmp_path)
    _reset_runtime_state()

    settings = get_settings()

    assert settings.DATABASE_URL.startswith("sqlite:///")
    assert settings.JWT_SECRET == "test-secret"
    assert settings.JWT_ALGORITHM == "HS256"
    assert settings.ACCESS_TOKEN_EXPIRE_MINUTES == 60
    assert settings.OCR_SERVICE_TIMEOUT == 30


def test_database_session_dependency_can_be_created(monkeypatch, tmp_path: Path) -> None:
    _configure_test_env(monkeypatch, tmp_path)
    _reset_runtime_state()

    db_dependency = get_db()
    session = next(db_dependency)
    try:
        assert session is not None
    finally:
        db_dependency.close()


def test_database_connection_can_initialize_with_test_config(monkeypatch, tmp_path: Path) -> None:
    _configure_test_env(monkeypatch, tmp_path)
    _reset_runtime_state()

    engine = get_engine()
    with engine.connect() as connection:
        result = connection.execute(text("SELECT 1"))
        assert result.scalar_one() == 1


def test_sql_server_connection_if_configured() -> None:
    _reset_runtime_state()
    database_url = get_settings().DATABASE_URL
    if not database_url.startswith("mssql+pyodbc://"):
        pytest.skip("SQL Server connectivity test requires DATABASE_URL with mssql+pyodbc.")

    engine = get_engine()
    with engine.connect() as connection:
        result = connection.execute(text("SELECT 1"))
        assert result.scalar_one() == 1
