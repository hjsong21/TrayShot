# CHANGELOG

언어: [English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](CHANGELOG.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](CHANGELOG.ko.md)

---

## [v0.1.0] - 2026-07-29

### 기능 추가 (Added)
- **코어 데이터 모델**: `Screenshot`, `ScreenshotChange`, `FileType` 도메인 모델 정의
- **SQLite 로컬 DB 래퍼**: WAL(Write-Ahead Logging) 모드 적용 고성능 스레드 세이프 DB 래퍼 구현
- **AppSettings & 로거**: JSON 직렬화 설정 싱글톤 인프라 및 비동기 파일 로거 구축
- **스크린샷 폴더 감시자**: Reactive Debounce 적용 `FolderWatcher` 및 `RecursiveFolderWatcher` 구현
- **중앙 상태 관리자**: 신규 파일 캡처 안착 검증(Settlement ladder) 및 `ScreenshotStore` 저장소 구축
