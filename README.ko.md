# TrayShot

언어: [English](README.md) | [한국어](README.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](README.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](README.ko.md)

TrayShot는 가볍고 빠르며 강력한 **Windows 스마트 스크린샷 갤러리 & 이미지 매니저**입니다. 캡처 폴더를 실시간으로 감지하여 날짜별 썸네일 그리드로 아름답게 보여주며, OCR 텍스트 검색, MobileCLIP 시맨틱 검색, 양방향 클립보드 연동, 무손실 자동 이미지 포맷 변환 등 다양한 편의 기능을 제공합니다.

---

## 주요 기능 (Key Features)

- 🖼️ **Fast 스크린샷 갤러리**: 날짜별 썸네일 그리드 뷰 및 초고속 패널 로딩.
- ⌨️ **전역 단축키 핫키**: 사용자 지정 단축키(기본값: `Ctrl + Alt + S`)로 언제 어디서나 갤러리 패널을 열고 닫을 수 있습니다.
- 🔍 **AI 기반 스크린샷 검색**:
  - **OCR 검색**: 스크린샷 내의 글자/텍스트를 백그라운드에서 자동으로 추출하여 키워드로 즉시 검색합니다.
  - **시맨틱 검색**: 로컬 AI 모델(MobileCLIP)을 기반으로 스크린샷의 시각적 문맥과 내용 묘사로 이미지를 검색합니다.
- ⏳ **이미지 포맷 변환**:
  - 수동 포맷 변환 지원 (PNG, JPG, WebP, BMP, GIF, TIFF, HEIC).
  - 새로 캡처된 PNG 스크린샷을 무손실 WebP로 자동 변환하며, 1:1 픽셀 일치 검증 및 용량 절감 확인 후 처분 정책(휴지통 이동, 영구 삭제, 보존)을 수행합니다.
- 📋 **클립보드 및 윈도우 탐색기 연동**:
  - `Ctrl + C` / `Ctrl + V`를 이용해 탐색기, 웹 브라우저, 메신저(카카오톡 등)와 자유롭게 스크린샷을 복사/붙여넣기할 수 있습니다.
  - `Ctrl + Z` 삭제 및 붙여넣기 실행 취소를 지원합니다.
- 🌐 **실시간 다국어 지원**: 앱 재시작 없이 설정 창에서 언어(한국어, 영어)를 즉시 변경할 수 있습니다.

---

## 시스템 요구 사항 (Requirements)

- **운영체제**: Windows 10 (버전 2004 / 빌드 19041) 이상.
- **런타임**: [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (WPF 지원).
- **빌드 도구**: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

---

## 빌드 및 실행 (Build & Run)

### 리포지토리 클론
```bash
git clone https://github.com/hjsong21/TrayShot.git
cd TrayShot
```

### 애플리케이션 빌드
```powershell
dotnet build
```

### 애플리케이션 실행
```powershell
dotnet run --project src/TrayShot/TrayShot.csproj
```

### 단위 테스트 실행
```powershell
dotnet test
```

---

## 설계 메모 (Design Notes)

- **아키텍처**: `CommunityToolkit.Mvvm` 라이브러리를 활용한 **MVVM (Model-View-ViewModel)** 패턴 준수.
- **UI 프레임워크**: 커스텀 다크 모드 브러시, 템플릿 마이크로 애니메이션 기반 Native WPF.
- **데이터베이스 & 인덱싱**: SQLite (`Microsoft.Data.Sqlite`)를 활용하여 OCR 텍스트 인덱스, 메타데이터, 시맨틱 벡터를 안전하게 보관 및 빠른 검색 지원.
- **파일 시스템 모니터링**: `FileSystemWatcher`를 통해 스크린샷 생성/변경을 비동기로 즉시 감지하여 갤러리에 반영.
- **WPF 다국어 (i18n)**: 동적 XAML `ResourceDictionary` 교체 방식을 적용하여 언어 변경 시 `SetResourceReference`를 통해 UI가 실시간으로 재로드됨.
- **안전한 포맷 변환 파이프라인**: WebP 변환 시 원본과 변환 파일의 픽셀 행(1:1 RGB/Alpha)을 비교 검증하고 용량 감소 확인 후 원본 처분을 실행.

---

## 감사 말씀 (Acknowledgements)

이 프로젝트는 Suhun Han(ssut)님이 개발하신 macOS 스크린샷 관리 앱 **[Sukurini](https://github.com/ssut/Sukurini)**의 아이디어와 핵심 로직을 바탕으로 윈도우 환경에 맞게 포팅(Porting)되었습니다. 
훌륭한 아이디어를 오픈소스로 공유해 주신 원작자님께 깊은 감사를 드립니다.

---

## 라이선스 (License)

아파치 라이선스 2.0 — 자세한 내용은 [LICENSE](LICENSE) 파일을 확인해 주세요. 
이 프로젝트의 포크(Fork)는 언제나 환영합니다. 단, 코드를 활용하실 때는 다음 사항을 지켜주셔야 합니다:
- 원본 저작권 표기를 유지해야 합니다.
- 코드를 수정한 경우 변경 사항을 명시해야 합니다.
- 제3자 라이선스 고지가 표시되는 모든 곳에 `NOTICE` 파일의 내용을 포함해야 합니다.

**주의:** "TrayShot"이라는 이름(상표)은 본 라이선스의 적용을 받지 않으며 무단으로 사용할 수 없습니다.

---

## 작성자 (Author)

- **Ho-Jeong Song** ([hjsong21](https://github.com/hjsong21))
