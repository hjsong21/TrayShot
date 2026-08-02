# CHANGELOG

언어: [English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](CHANGELOG.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](CHANGELOG.ko.md)

---

## [v0.9.0] - 2026-08-02

### 기능 추가 (Added)
- **날짜별 그룹화 UI 패널**: 갤러리 패널 내 스크린샷을 생성 날짜별(오늘, 어제, 이번 주 등) 섹션 헤더로 분리 표시
- **2D 그리드 키보드 탐색**: 그룹 내 그리드 행/열 구조 계산 기반의 2차원 수직/수평 키보드 탐색 엔진 구현
- **Home/End/PgUp/PgDn 고속 이동**: 전체 맨 첫 항목, 맨 끝 항목 및 1페이지 단위 고속 이동 및 자동 스크롤 연동

### 기능 개선 (Improved)
- **선호 열 위치 유지**: 그룹 경계를 넘나들 때 기존 열(Column) 위치를 기억하고 유지하는 `GridRowInfo` 알고리즘 적용
- **미리보기 뷰어 커서 동기화**: `PreviewWindow`에서 이미지 이동 시 메인 갤러리 패널의 선택 커서 및 스크롤 위치 연동
- **정교한 날짜 그룹 정렬**: 실제 파일 생성 일시(`CreationTime`) 및 스크린샷 파일명 패턴 파싱 결합

### 버그 수정 (Fixed)
- **그룹화 뷰 커맨드 바인딩 수정**: 날짜 그룹 적용 후 우클릭 컨텍스트 메뉴 명령(복사, 변환, 삭제 등) 단절 문제 해결
- **삭제 커서 선택 테두리 수정**: Delete 삭제 직후 다음 항목의 파란색 선택 테두리가 비주얼에 즉시 칠해지도록 보장

## [v0.8.0] - 2026-07-31

### 기능 추가 (Added)
- **About 팝업 창 구현**: 앱 버전, 저작권, 핵심 기능 하이라이트 목록을 안내하는 `AboutWindow` 작성
- **정보 아이콘 & 트레이 연동**: 갤러리 헤더 정보 버튼 및 시스템 트레이 우클릭 메뉴의 '앱 정보' 항목 바인딩

### 기능 개선 (Improved)
- **메타데이터 동적 파싱**: `Sukurini.csproj` 내의 Version 및 Copyright 태그 값을 Assembly 특성에서 동적으로 파싱하여 UI 반영
- **설정 창 위치 중앙 상단 정렬**: `PreferencesWindow` 팝업 위치가 메인 갤러리 창의 중앙 상단에 자동으로 정렬되도록 조정
- **설정 창 ESC 닫기 지원**: `PreferencesWindow`에 KeyDown 핸들러를 연결하여 ESC 키 누름 시 즉시 닫기 지원
- **삭제 후 다음 항목 자동 선택**: 스크린샷 삭제 시 선택 해제 없이 다음 순서 이미지 카드가 자동 선택되도록 개선
- **방향키 상대 이동 UX**: 키보드 방향키 이동 시 현재 선택 항목을 기준으로 이전/다음 이동 및 자동 스크롤 연동

### 버그 수정 (Fixed)
- **AppSettings Enum 역직렬화 수정**: JSON 직렬화 옵션에 `JsonStringEnumConverter`를 추가하여 처분 정책 리셋 버그 해결
- **탭 마우스오버 플리커링 수정**: 설정 창 탭 마우스 호버 시 배경 브러시가 순간적으로 깜빡이던 ControlTemplate 스타일 이슈 수정

## [v0.7.0] - 2026-07-30

### 기능 추가 (Added)
- **우클릭 컨텍스트 메뉴**: 갤러리 카드 우클릭 메뉴 구현 (열기, 복사, 붙여넣기, 포맷 변환, 삭제, Undo)
- **6가지 수동 포맷 변환**: PNG, JPG, WebP, BMP, GIF, TIFF 포맷으로 즉시 수동 변환하는 서브메뉴 구성
- **HEIC 이미지 코덱 연동**: Windows WIC 코덱 파이프라인 연동을 통한 HEIC 디코딩 및 타 포맷 상호 변환 지원
- **포맷 변환 토스트 알림**: 변환 진행 중('⏳ 변환 중...'), 성공 완료('✅ 변환 완료!') 안내 토스트 UI 연동

### 기능 개선 (Improved)
- **원본 타임스탬프 보존**: 수동 변환 후 생성된 파일의 `CreationTime` 및 `LastWriteTime`을 원본과 동일하게 유지
- **썸네일 카드 포맷 뱃지**: 각 카드 우측 상단 영역에 확장자(PNG, WEBP, JPG 등) 표시 비주얼 뱃지 디자인 적용

### 성능 개선 (Performance)
- **비동기 변환 처리**: 대용량 변환 시 UI 스레드 멈춤 현상 방지를 위한 `Task.Run` 비동기 이벤팅 전환

## [v0.6.0] - 2026-07-30

### 기능 추가 (Added)
- **Win32 휴지통 이동 삭제**: Delete 키 누름 시 `SHFileOperation` API를 활용하여 윈도우 휴지통으로 안전 삭제
- **클립보드 양방향 연동**: Ctrl+C 누름 시 파일 경로 및 비트맵 동시 복사, Ctrl+V 누름 시 외부 이미지 캡처 폴더 붙여넣기
- **Ctrl+Z 실행 취소 스택**: 붙여넣은 파일 제거 및 Delete 삭제 건 휴지통 복구(Undo) 메인 메모리 스택 구현

## [v0.5.0] - 2026-07-30

### 기능 추가 (Added)
- **미리보기 뷰어 키보드 탐색**: `PreviewWindow` 팝업 내 좌/우/Home/End 방향키 이미지 탐색 지원
- **검색바 초기화 버튼**: 검색창 텍스트 1자 이상 입력 시 활성화되는 지우기(X) 버튼 및 커맨드 구현
- **다단계 ESC 키 처리**: ESC 키 누름 시 1단계 검색어 초기화 ➔ 2단계 갤러리 창 숨기기 로직 적용

### 기능 개선 (Improved)
- **뷰어 오픈 포커스 원복**: `PreviewWindow` 오픈 시 메인 갤러리 창 유지 및 뷰어 닫힘 시 포커스 자동 복원
- **헤더 아이콘 리소스 갱신**: 갤러리 헤더 설정/종료 버튼 비주얼을 깔끔한 SVG 벡터 아이콘으로 교체

## [v0.4.0] - 2026-07-30

### 기능 추가 (Added)
- **Win32 전역 단축키**: `RegisterHotKey` API 연동을 통한 바탕화면 전역 단축키(`Ctrl+Alt+S`) 갤러리 패널 토글
- **실시간 핫키 입력 컨트롤**: 설정 창에서 사용자 지정 키 조합을 입력받고 중복 충돌을 실시간 검증하는 컨트롤 구현

### 버그 수정 (Fixed)
- **HWND 지연 생성 수정**: 윈도우 숨김 상태에서 전역 단축키 등록이 실패하던 문제를 `EnsureHandle()` 호출로 해결
- **단축키 차단 방지**: 단일 수식어 키(Ctrl/Shift/Alt) 입력 시 기존 시스템 단축키 오작동 방지 가드 추가
- **무한 재변환 방지**: 원본 PNG 보존(Keep) 정책 사용 시 발생하던 WebP 무한 재변환 버그 수정

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
