using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class EvasiveCircleBot : Bot
{
    // Arah gerak bot, 1 untuk maju dan -1 untuk mundur
    int moveDir = 1;

    // Arah belok bot, 1 ke kanan dan -1 ke kiri
    int turnDir = 1;

    static void Main(string[] args)
    {
        // Menjalankan bot EvasiveCircleBot
        new EvasiveCircleBot().Start();
    }

    EvasiveCircleBot() : base(BotInfo.FromFile("alt.json")) { }

    public override void Run()
    {
        // Mengatur warna tampilan bot
        BodyColor = Color.DarkBlue;
        TurretColor = Color.Cyan;
        RadarColor = Color.Yellow;
        BulletColor = Color.White;

        // Mengatur kecepatan maksimal bot
        MaxSpeed = 7;

        // Radar muter terus buat mencari musuh
        SetTurnRadarRight(Double.PositiveInfinity);

        // Loop utama selama bot masih hidup
        while (IsRunning)
        {
            // Bot bergerak melingkar
            SetTurnRight(35 * turnDir);

            // Bot maju atau mundur sesuai arah geraknya
            SetForward(250 * moveDir);

            // Gun diputar pelan-pelan sambil mencari target
            SetTurnGunRight(25);

            // Menjalankan semua perintah
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Kalau yang discan adalah teman, abaikan
        if (IsTeammate(e.ScannedBotId)) return;

        // Menghitung jarak musuh
        double distance = DistanceTo(e.X, e.Y);

        // Menghitung sudut gun terhadap posisi musuh
        double gunBearing = GunBearingTo(e.X, e.Y);

        // Mengarahkan gun ke musuh
        SetTurnGunLeft(gunBearing);

        // Kalau aim cukup dekat dan gun siap, bot akan menembak
        if (Math.Abs(gunBearing) < 8 && GunHeat == 0)
        {
            // Musuh dekat dan energy cukup, pakai tembakan paling kuat
            if (distance < 120 && Energy > 30)
                Fire(3);

            // Musuh jarak sedang, pakai power sedang
            else if (distance < 350)
                Fire(2);

            // Musuh jauh, pakai power kecil biar hemat
            else
                Fire(1);
        }

        // Kalau musuh terlalu dekat, bot mundur buat jaga jarak
        if (distance < 180)
        {
            moveDir *= -1;
            SetBack(180);
        }

        // Scan ulang supaya musuh tetap terpantau
        Rescan();
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Kalau kena peluru, langsung ubah arah gerak dan arah belok
        moveDir *= -1;
        turnDir *= -1;

        // Belok tajam untuk menghindari tembakan berikutnya
        SetTurnRight(70 * turnDir);

        // Bergerak ke arah baru
        SetForward(220 * moveDir);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Kalau nabrak tembok, ganti arah gerak
        moveDir *= -1;

        // Mundur supaya tidak nyangkut di tembok
        SetBack(180);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Kalau bot ini menabrak bot lain, mundur
        if (e.IsRammed)
        {
            moveDir *= -1;
            SetBack(120);
        }

        // Arahkan gun ke bot yang ditabrak
        double gunBearing = GunBearingTo(e.X, e.Y);
        TurnGunLeft(gunBearing);

        // Kalau gun siap, tembak dengan power penuh
        if (GunHeat == 0)
            Fire(3);
    }
}