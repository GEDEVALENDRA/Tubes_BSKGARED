using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class BSKLASKARPEMDA : Bot
{
    // Arah putaran bot, 1 ke kanan dan -1 ke kiri
    int turnDirection = 1;

    // Jarak ideal bot dari musuh
    const double SAFE_DISTANCE = 300;

    // Toleransi jarak agar bot tidak terlalu sering maju-mundur
    const double DISTANCE_TOLERANCE = 20;

    static void Main(string[] args)
    {
        // Menjalankan bot BSKLASKARPEMDA
        new BSKLASKARPEMDA().Start();
    }

    BSKLASKARPEMDA() : base(BotInfo.FromFile("alt.json")) { }

    public override void Run()
    {
        // Mengatur warna bot
        BodyColor = Color.FromArgb(0x99, 0x99, 0x99);
        TurretColor = Color.FromArgb(0x88, 0x88, 0x88);
        RadarColor = Color.FromArgb(0x66, 0x66, 0x66);
        ScanColor = Color.Yellow;

        // Gun dan radar tetap stabil walaupun body/gun berputar
        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        // Kecepatan maksimum bot
        MaxSpeed = 8;

        // Loop utama selama bot masih berjalan
        while (IsRunning)
        {
            // Radar berputar penuh untuk mencari musuh
            TurnRadarRight(360);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Menghitung jarak bot ke musuh
        double distance = DistanceTo(e.X, e.Y);

        // Menghitung posisi gun terhadap musuh
        double gunBearing = GunBearingTo(e.X, e.Y);

        // Mengarahkan gun ke musuh
        TurnGunLeft(gunBearing);

        // Kalau gun sudah siap dan arahnya cukup pas, bot menembak
        if (GunHeat == 0 && Abs(GunBearingTo(e.X, e.Y)) < 10)
        {
            Fire(3);
        }

        // Kalau musuh terlalu dekat, bot mundur
        if (distance < SAFE_DISTANCE - DISTANCE_TOLERANCE)
        {
            TurnRight(25 * turnDirection);
            Back(120);
        }

        // Kalau musuh terlalu jauh, bot maju mendekat
        else if (distance > SAFE_DISTANCE + DISTANCE_TOLERANCE)
        {
            TurnRight(20 * turnDirection);
            Forward(80);
        }

        // Kalau jaraknya sudah pas, bot tetap bergerak kecil
        else
        {
            TurnRight(15 * turnDirection);
            Forward(50);
        }

        // Scan ulang agar musuh tetap terdeteksi
        Rescan();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Saat menabrak bot lain, arahkan gun ke bot tersebut
        TurnGunLeft(GunBearingTo(e.X, e.Y));

        // Kalau gun siap, tembak dengan power penuh
        if (GunHeat == 0)
        {
            Fire(3);
        }

        // Mundur setelah tabrakan
        Back(140);

        // Belok supaya tidak terus menempel ke musuh
        TurnRight(45 * turnDirection);

        // Balik arah putaran agar gerakan tidak monoton
        turnDirection *= -1;

        // Scan ulang
        Rescan();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Kalau nabrak tembok, bot mundur
        Back(120);

        // Lalu belok agar keluar dari tembok
        TurnRight(90 * turnDirection);

        // Balik arah putaran
        turnDirection *= -1;

        // Scan ulang
        Rescan();
    }

    private double Abs(double value)
    {
        // Fungsi manual untuk mengambil nilai absolut
        // Contoh: -5 jadi 5, sedangkan 5 tetap 5
        return value < 0 ? -value : value;
    }
}