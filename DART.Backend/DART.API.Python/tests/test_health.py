from fastapi.testclient import TestClient

from app.core.config import clear_settings_cache
from app.database.connection import clear_database_state
from app.main import app


def test_health(monkeypatch, tmp_path) -> None:
    db_file = tmp_path / "test_health.db"
    monkeypatch.setenv("DATABASE_URL", f"sqlite:///{db_file.as_posix()}")
    clear_database_state()
    clear_settings_cache()

    with TestClient(app) as client:
        response = client.get("/health")

    assert response.status_code == 200
    assert response.json()["status"] == "ok"
