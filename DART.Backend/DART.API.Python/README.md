# DART.API.Python

Backend API for DART (Document Archiving and Retrieval Tool), a local web-based document archiving and retrieval system for the Local Civil Registrar's Office of San Pascual, Batangas.

## Purpose

This repository contains the Python backend API that will expose REST endpoints for DART workflows. This stage includes project scaffolding, environment-based configuration, SQLAlchemy database foundation, Alembic setup, and a health check endpoint.

## Technology Stack

- Python 3.12+
- FastAPI
- Uvicorn
- SQLAlchemy
- Alembic
- Pydantic and Pydantic Settings
- JWT support (python-jose)
- Password hashing libraries (argon2-cffi and bcrypt)
- pytest and HTTPX
- python-multipart

## Setup

### 1. Create a virtual environment

On Windows PowerShell:

```powershell
python -m venv .venv
```

### 2. Activate the virtual environment

On Windows PowerShell:

```powershell
.\.venv\Scripts\Activate.ps1
```

### 3. Install dependencies

```powershell
pip install -r requirements.txt
```

### 4. Configure environment variables

Copy `.env.example` to `.env` and set values:

```powershell
Copy-Item .env.example .env
```

Do not commit `.env`.

## Environment Variables

Set these values in `.env`:

```env
DATABASE_URL=
JWT_SECRET=
JWT_ALGORITHM=HS256
ACCESS_TOKEN_EXPIRE_MINUTES=60
DOCUMENT_ROOT=
OCR_SERVICE_URL=
OCR_SERVICE_TIMEOUT=30
```

Example local development database URL (SQLite file):

```env
DATABASE_URL=sqlite:///./dart_api.db
```

## Run the API

```powershell
uvicorn app.main:app --reload
```

API base URL is typically `http://127.0.0.1:8000`.

## API Docs

- Swagger UI: `/docs`
- ReDoc: `/redoc`

## Database and Migrations

Alembic is configured and uses the application settings (`DATABASE_URL`) through `app.core.config`.

When models are added later, generate a migration with:

```powershell
alembic revision --autogenerate -m "describe_change"
```

Apply migrations with:

```powershell
alembic upgrade head
```

No migrations are created yet in this stage because no application models exist.

## Run Tests

```powershell
pytest
```