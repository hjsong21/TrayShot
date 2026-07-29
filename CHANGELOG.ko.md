# CHANGELOG

언어: [English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](CHANGELOG.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](CHANGELOG.ko.md)

---

## [v0.3.0] - 2026-07-29

### 기능 추가 (Added)
- **커스텀 리사이즈 핸들**: 보더리스 윈도우 테두리 드래그 크기 조절을 위한 WM_NCHITTEST 네이티브 핸들 적용
- **창 크기 영구 보존**: 변경된 갤러리 창 너비/높이를 AppSettings에 영구 저장 및 초기화 기능 구현
- **동적 테마 스위칭**: 다크/라이트/시스템 테마 실시간 전환을 위한 `DynamicResource` 바인딩 적용
- **무손실 WebP 변환 파이프라인**: 1:1 RGB/Alpha 픽셀 대조 검증 기반 자동 WebP 변환 파이프라인 구축
- **원본 PNG 처분 정책**: WebP 변환 성공 시 원본 PNG 휴지통 이동(Trash), 영구 삭제(Delete), 보존(Keep) 선택 기능

### 버그 수정 (Fixed)
- **App.xaml 리소스 수정**: XAML 데이터 바인딩 시 발생하던 `BooleanToVisibilityConverter` 리소스 누락 예외 해결
- **변환 파이프라인 연결**: `ScreenshotStore` 파일 감지 이벤트와 `ConversionCoordinator` 상호 연동

## [v0.2.0] - 2026-07-29

### 기능 추가 (Added)
- **비동기 썸네일 디코딩**: ImageSharp 및 ConcurrentDictionary 기반 고속 이미지 디코딩 및 메모리 캐시 로더 구현
- **트레이 애니메이션 렌더러**: 스크린샷 감지 시 알림 영역 트레이 아이콘에 30FPS 동적 펄스 링 렌더링
- **스팟라이트 갤러리 UI**: macOS Spotlight 스타일의 보더리스 갤러리 패널 UI 및 아크릴 테마 레이아웃 구축
- **WinRT OCR 텍스트 추출**: Windows.Media.Ocr 엔진 연동을 통한 스크린샷 내 한글/영어 텍스트 자동 추출
- **FTS5 삼중자 검색**: SQLite FTS5 Trigram 전문 검색 인덱서 연동을 통한 부분 단어 및 초성 검색 지원
- **MobileCLIP AI 시맨틱 검색**: ONNX MobileCLIP 딥러닝 모델 및 BPE 토크나이저 기반 자연어 이미지 시맨틱 검색
- **Fluent Design 설정 UI**: WPF-UI 기반 모던 설정 윈도우(`PreferencesWindow`) 레이아웃 및 탭 구성
- **시작 프로그램 & Sentry 연동**: 레지스트리 자동 시작 제어(`StartupManager`) 및 Sentry 텔레메트리 연동

## [v0.1.0] - 2026-07-29

### 기능 추가 (Added)
- **코어 데이터 모델**: `Screenshot`, `ScreenshotChange`, `FileType` 도메인 모델 정의
- **SQLite 로컬 DB 래퍼**: WAL(Write-Ahead Logging) 모드 적용 고성능 스레드 세이프 DB 래퍼 구현
- **AppSettings & 로거**: JSON 직렬화 설정 싱글톤 인프라 및 비동기 파일 로거 구축
- **스크린샷 폴더 감시자**: Reactive Debounce 적용 `FolderWatcher` 및 `RecursiveFolderWatcher` 구현
- **중앙 상태 관리자**: 신규 파일 캡처 안착 검증(Settlement ladder) 및 `ScreenshotStore` 저장소 구축
