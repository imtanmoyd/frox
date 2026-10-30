from __future__ import annotations

import uvicorn

from frox_engine.app import create_app
from frox_engine.config import load_settings


def run() -> None:
    settings = load_settings()
    uvicorn.run(
        create_app(settings),
        host=settings.host,
        port=settings.port,
        log_level="info",
    )


if __name__ == "__main__":
    run()
