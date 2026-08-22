using System;
using Velopack;

namespace TrayShot;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack 커맨드라인 훅 처리 (설치/제거/업데이트 시 실행 후 자동 종료)
        VelopackApp.Build().Run();

        // 일반 앱 실행
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
