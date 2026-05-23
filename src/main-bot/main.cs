using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class BSKGARED : Bot
{
    // Arah gerak maju/mundur bot
    int moveDirection = 1;

    // Arah gerak menyamping untuk dodge
    int strafeDirection = 1;

    // Menyimpan energy musuh sebelumnya
    double lastEnemyEnergy = 100;

    // Jarak aman dari tembok
    const double WALL_MARGIN = 90;

    // Jarak bahaya untuk prediksi nabrak tembok
    const double DANGER_MARGIN = 130;

    static void Main(string[] args)
    {
        // Menjalankan bot BSKGARED
        new BSKGARED().Start();
    }

    BSKGARED() : base(BotInfo.FromFile("main.json")) { }

    public override void Run()
    {
        // Mengatur warna bagian-bagian bot
        BodyColor = Color.DarkGoldenrod;
        TurretColor = Color.Gray;
        RadarColor = Color.DarkTurquoise;
        BulletColor = Color.Red;
        ScanColor = Color.DarkRed;

        // Gun dan radar tetap stabil walau body bot berputar
        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;

        // Kecepatan maksimum bot
        MaxSpeed = 8;

        // Loop utama selama bot masih hidup
        while (IsRunning)
        {
            // Radar terus berputar untuk mencari musuh
            SetTurnRadarRight(360);

            // Gerak sambil menghindari tembok
            AvoidWallRoute();

            // Menjalankan semua perintah gerakan
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // Menghitung jarak dan arah musuh
        double distance = DistanceTo(e.X, e.Y);
        double absoluteBearing = DirectionTo(e.X, e.Y);

        // Mengunci radar ke arah musuh
        double radarTurn = NormalizeRelativeAngle(absoluteBearing - RadarDirection);
        SetTurnRadarLeft(radarTurn * 2.5);

        // Mengecek apakah energy musuh turun, artinya kemungkinan musuh menembak
        double energyDrop = lastEnemyEnergy - e.Energy;

        if (energyDrop > 0.09 && energyDrop <= 3.01)
        {
            // Kalau musuh menembak, bot langsung ganti arah untuk dodge
            moveDirection *= -1;
            strafeDirection *= -1;

            SetForward(190 * moveDirection);
        }

        // Update energy musuh terakhir
        lastEnemyEnergy = e.Energy;

        // Kalau dekat tembok, bot pindah ke jalur aman
        if (IsNearWall() || WillHitWall(170))
        {
            MoveToSafeRoute();
        }
        else
        {
            // Gerakan orbit, yaitu mengitari musuh
            double orbitAngle = 90 - (22 * moveDirection);

            double bodyTurn =
                NormalizeRelativeAngle(
                    absoluteBearing + orbitAngle - Direction
                );

            SetTurnLeft(bodyTurn);

            // Atur jarak ideal dari musuh
            if (distance > 300)
                SetForward(210 * moveDirection);
            else if (distance < 170)
                SetBack(150);
            else
                SetForward(150 * moveDirection);
        }

        // Offset kecil untuk aim berdasarkan jarak musuh
        double aimOffset;

        if (distance > 450)
        {
            aimOffset = -2;
        }
        else if (distance > 250)
        {
            aimOffset = -1;
        }
        else if (distance > 120)
        {
            aimOffset = 0;
        }
        else
        {
            aimOffset = 0.4;
        }

        // Mengarahkan gun ke musuh
        double gunTurn =
            NormalizeRelativeAngle(
                (absoluteBearing - GunDirection) + aimOffset
            );

        SetTurnGunLeft(gunTurn);

        // Menembak kalau gun sudah dingin dan arah tembakan cukup pas
        if (GunHeat == 0 && Math.Abs(gunTurn) < GetAimTolerance(distance))
        {
            double power = GetFirePower(distance);

            // Jangan sampai energy bot habis total karena menembak
            Fire(Math.Min(power, Energy - 0.2));
        }

        // Scan ulang musuh
        Rescan();
    }

    void AvoidWallRoute()
    {
        // Kalau dekat tembok atau diprediksi bakal nabrak,
        // bot langsung cari jalur aman
        if (IsNearWall() || WillHitWall(130))
        {
            MoveToSafeRoute();
        }
        else
        {
            // Gerakan normal: sedikit belok sambil maju
            SetTurnRight(25 * strafeDirection);
            SetForward(160 * moveDirection);
        }
    }

    bool IsNearWall()
    {
        // Mengecek apakah posisi bot terlalu dekat dengan batas arena
        return X < WALL_MARGIN ||
               Y < WALL_MARGIN ||
               X > ArenaWidth - WALL_MARGIN ||
               Y > ArenaHeight - WALL_MARGIN;
    }

    bool WillHitWall(double distanceAhead)
    {
        // Mengubah arah bot dari derajat ke radian
        double rad = Direction * Math.PI / 180.0;

        // Prediksi posisi X bot jika maju sejauh distanceAhead
        double nextX =
            X + Math.Sin(rad) * distanceAhead * moveDirection;

        // Prediksi posisi Y bot jika maju sejauh distanceAhead
        double nextY =
            Y + Math.Cos(rad) * distanceAhead * moveDirection;

        // Return true kalau posisi prediksi terlalu dekat dengan tembok
        return nextX < DANGER_MARGIN ||
               nextY < DANGER_MARGIN ||
               nextX > ArenaWidth - DANGER_MARGIN ||
               nextY > ArenaHeight - DANGER_MARGIN;
    }

    void MoveToSafeRoute()
    {
        // Titik tengah arena sebagai arah aman
        double centerX = ArenaWidth / 2.0;
        double centerY = ArenaHeight / 2.0;

        // Arah menuju tengah arena
        double safeDirection = DirectionTo(centerX, centerY);

        // Hitung sudut belok menuju arah aman
        double turn =
            NormalizeRelativeAngle(
                safeDirection - Direction
            );

        // Default-nya bot maju
        moveDirection = 1;

        // Kalau sudut belok terlalu besar,
        // bot lebih efisien mundur daripada muter jauh
        if (Math.Abs(turn) > 90)
        {
            turn =
                NormalizeRelativeAngle(turn + 180);

            moveDirection = -1;
        }

        // Ubah arah strafing biar gerakannya tidak monoton
        strafeDirection *= -1;

        // Belok ke arah aman lalu bergerak
        SetTurnLeft(turn);
        SetForward(220 * moveDirection);
    }

    double GetFirePower(double distance)
    {
        // Kalau energy rendah, pakai peluru kecil biar hemat
        if (Energy < 15)
            return 1.2;

        // Semakin dekat musuh, semakin besar power tembakan
        if (distance < 90)
            return 3.0;

        if (distance < 180)
            return 2.7;

        if (distance < 350)
            return 2.1;

        // Kalau musuh jauh, pakai power kecil agar tidak boros
        return 1.4;
    }

    double GetAimTolerance(double distance)
    {
        // Musuh dekat: toleransi aim lebih besar
        if (distance < 120)
            return 14;

        if (distance < 250)
            return 9;

        if (distance < 450)
            return 5;

        // Musuh jauh: aim harus lebih presisi
        return 3;
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Saat kena peluru, bot langsung ganti arah gerak
        moveDirection *= -1;
        strafeDirection *= -1;

        // Belok menjauh/menyamping dari arah peluru
        TurnLeft(
            NormalizeRelativeAngle(
                90 - (Direction - e.Bullet.Direction)
            )
        );

        // Bergerak untuk menghindari tembakan berikutnya
        SetForward(220 * moveDirection);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        // Kalau nabrak tembok, ubah arah gerak
        moveDirection *= -1;
        strafeDirection *= -1;

        // Mundur dan belok agar tidak nyangkut
        SetBack(180);
        SetTurnRight(100);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Saat menabrak bot lain, body diarahkan ke musuh
        TurnLeft(BearingTo(e.X, e.Y));

        // Gun diarahkan langsung ke musuh
        TurnGunLeft(GunBearingTo(e.X, e.Y));

        // Kalau gun siap, tembak dengan power maksimal
        if (GunHeat == 0)
            Fire(3);

        // Mundur setelah tabrakan
        SetBack(100);

        // Ubah arah gerak secara agresif
        moveDirection *= -3;
        strafeDirection *= -3;
    }

    public override void OnWonRound(WonRoundEvent e)
    {
        // Selebrasi kecil saat menang ronde
        TurnRight(360);
    }
}