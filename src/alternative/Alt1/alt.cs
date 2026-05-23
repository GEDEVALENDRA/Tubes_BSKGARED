using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class RamFire : Bot
{
    int moveDirection = 1;
    int strafeDirection = 1;

    const double WALL_MARGIN = 90;
    const double DANGER_MARGIN = 130;

    Dictionary<int, double> enemyEnergy = new Dictionary<int, double>();

    int targetId = -1;
    double targetEnergy = 999;
    double targetDistance = 9999;

    double lastX = 0;
    double lastY = 0;
    int stuckCounter = 0;

    static void Main(string[] args)
    {
        new RamFire().Start();
    }

    RamFire() : base(BotInfo.FromFile("alt.json")) { }

    public override void Run()
    {
        BodyColor = Color.DarkGoldenrod;
        TurretColor = Color.Gray;
        RadarColor = Color.DarkTurquoise;
        BulletColor = Color.Red;
        ScanColor = Color.DarkRed;

        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;

        MaxSpeed = 8;

        lastX = X;
        lastY = Y;

        while (IsRunning)
        {
            CheckStuck();

            if (EnemyCount > 1)
            {
                SetTurnRadarRight(360);
                MeleeMovement();
            }
            else
            {
                SetTurnRadarRight(360);
                AvoidWallRoute();
            }

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double distance = DistanceTo(e.X, e.Y);
        double absoluteBearing = DirectionTo(e.X, e.Y);

        // ================= ENEMY ENERGY PER BOT =================
        if (!enemyEnergy.ContainsKey(e.ScannedBotId))
            enemyEnergy[e.ScannedBotId] = e.Energy;

        double energyDrop = enemyEnergy[e.ScannedBotId] - e.Energy;

        if (energyDrop > 0.09 && energyDrop <= 3.01)
        {
            moveDirection *= -1;
            strafeDirection *= -1;
            SetForward(180 * moveDirection);
        }

        enemyEnergy[e.ScannedBotId] = e.Energy;

        // ================= TARGET SELECTION =================
        bool betterTarget =
            targetId == -1 ||
            e.Energy < targetEnergy ||
            distance < targetDistance - 120;

        if (betterTarget)
        {
            targetId = e.ScannedBotId;
            targetEnergy = e.Energy;
            targetDistance = distance;
        }

        // Kalau melee, jangan terlalu lock radar
        if (EnemyCount <= 1 || e.ScannedBotId == targetId)
        {
            double radarTurn = NormalizeRelativeAngle(absoluteBearing - RadarDirection);
            SetTurnRadarLeft(radarTurn * 2.2);
        }

        // ================= MOVEMENT =================
        if (EnemyCount > 1)
        {
            CrossfireMovement(e.X, e.Y, distance, absoluteBearing);
        }
        else
        {
            DuelMovement(distance, absoluteBearing);
        }

        // ================= AIM =================
        if (e.ScannedBotId == targetId || EnemyCount <= 1)
        {
            AimAndFire(e, distance, absoluteBearing);
        }

        Rescan();
    }

    void AimAndFire(ScannedBotEvent e, double distance, double absoluteBearing)
    {
        double bulletPower;

        if (IsStuck())
            bulletPower = 3.0;
        else
            bulletPower = GetFirePower(distance);

        double aimOffset = 0;

        if (EnemyCount > 1)
        {
            if (distance > 450) aimOffset = -1.5;
            else if (distance > 250) aimOffset = -0.7;
            else aimOffset = 0;
        }
        else
        {
            if (distance > 450) aimOffset = -2;
            else if (distance > 250) aimOffset = -1;
            else if (distance > 120) aimOffset = 0;
            else aimOffset = 0.4;
        }

        double gunTurn = NormalizeRelativeAngle(
            absoluteBearing - GunDirection + aimOffset
        );

        SetTurnGunLeft(gunTurn);

        if (GunHeat == 0 && Math.Abs(gunTurn) < GetAimTolerance(distance))
        {
            Fire(Math.Min(bulletPower, Math.Max(0.1, Energy - 0.2)));
        }
    }

    // ================= MELEE MOVEMENT =================

    void MeleeMovement()
    {
        if (IsNearWall() || WillHitWall(150))
        {
            MoveToSafeRoute();
            return;
        }

        double centerBearing = DirectionTo(ArenaWidth / 2.0, ArenaHeight / 2.0);
        double awayFromCenter = NormalizeRelativeAngle(centerBearing + 180 - Direction);

        SetTurnLeft(awayFromCenter + (35 * strafeDirection));
        SetForward(170 * moveDirection);
    }

    void CrossfireMovement(double enemyX, double enemyY, double distance, double absoluteBearing)
    {
        if (IsNearWall() || WillHitWall(160))
        {
            MoveToSafeRoute();
            return;
        }

        // Bergerak menyamping agar musuh sering salah tembak / crossfire
        double orbitAngle;

        if (distance > 420)
            orbitAngle = 70;
        else if (distance > 220)
            orbitAngle = 95;
        else
            orbitAngle = 120;

        double turn = NormalizeRelativeAngle(
            absoluteBearing + (orbitAngle * strafeDirection) - Direction
        );

        SetTurnLeft(turn);

        if (distance < 150)
        {
            SetBack(170);
            moveDirection *= -1;
        }
        else
        {
            SetForward(190 * moveDirection);
        }

        // Random kecil supaya tidak gampang diprediksi
        if (Random.Shared.NextDouble() < 0.04)
        {
            moveDirection *= -1;
            strafeDirection *= -1;
        }
    }

    void DuelMovement(double distance, double absoluteBearing)
    {
        if (IsNearWall() || WillHitWall(170))
        {
            MoveToSafeRoute();
            return;
        }

        double orbitAngle = 90 - (22 * moveDirection);

        double bodyTurn = NormalizeRelativeAngle(
            absoluteBearing + orbitAngle - Direction
        );

        SetTurnLeft(bodyTurn);

        if (distance > 300)
            SetForward(210 * moveDirection);
        else if (distance < 170)
            SetBack(150);
        else
            SetForward(150 * moveDirection);
    }

    // ================= STUCK SYSTEM =================

    void CheckStuck()
    {
        double moved = DistanceTo(lastX, lastY);

        if (moved < 1.2)
            stuckCounter++;
        else
            stuckCounter = 0;

        lastX = X;
        lastY = Y;

        if (IsStuck())
        {
            moveDirection *= -1;
            strafeDirection *= -1;

            SetTurnRight(120);
            SetBack(220);
        }
    }

    bool IsStuck()
    {
        return stuckCounter > 18;
    }

    // ================= WALL SYSTEM =================

    void AvoidWallRoute()
    {
        if (IsNearWall() || WillHitWall(130))
        {
            MoveToSafeRoute();
        }
        else
        {
            SetTurnRight(25 * strafeDirection);
            SetForward(160 * moveDirection);
        }
    }

    bool IsNearWall()
    {
        return X < WALL_MARGIN ||
               Y < WALL_MARGIN ||
               X > ArenaWidth - WALL_MARGIN ||
               Y > ArenaHeight - WALL_MARGIN;
    }

    bool WillHitWall(double distanceAhead)
    {
        double rad = Direction * Math.PI / 180.0;

        double nextX = X + Math.Sin(rad) * distanceAhead * moveDirection;
        double nextY = Y + Math.Cos(rad) * distanceAhead * moveDirection;

        return nextX < DANGER_MARGIN ||
               nextY < DANGER_MARGIN ||
               nextX > ArenaWidth - DANGER_MARGIN ||
               nextY > ArenaHeight - DANGER_MARGIN;
    }

    void MoveToSafeRoute()
    {
        double centerX = ArenaWidth / 2.0;
        double centerY = ArenaHeight / 2.0;

        double safeDirection = DirectionTo(centerX, centerY);

        double turn = NormalizeRelativeAngle(safeDirection - Direction);

        moveDirection = 1;

        if (Math.Abs(turn) > 90)
        {
            turn = NormalizeRelativeAngle(turn + 180);
            moveDirection = -1;
        }

        strafeDirection *= -1;

        SetTurnLeft(turn);
        SetForward(230 * moveDirection);
    }

    // ================= FIRE POWER =================

    double GetFirePower(double distance)
    {
        if (Energy < 10)
            return 1.0;

        if (distance < 90)
            return 3.0;

        if (distance < 180)
            return 2.6;

        if (distance < 350)
            return 2.0;

        return 1.3;
    }

    double GetAimTolerance(double distance)
    {
        if (IsStuck())
            return 20;

        if (distance < 120)
            return 14;

        if (distance < 250)
            return 9;

        if (distance < 450)
            return 5;

        return 3;
    }

    // ================= EVENTS =================

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        moveDirection *= -1;
        strafeDirection *= -1;

        SetTurnLeft(
            NormalizeRelativeAngle(
                90 - (Direction - e.Bullet.Direction)
            )
        );

        SetForward(220 * moveDirection);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        stuckCounter += 10;

        moveDirection *= -1;
        strafeDirection *= -1;

        SetBack(200);
        SetTurnRight(120);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        TurnLeft(BearingTo(e.X, e.Y));
        TurnGunLeft(GunBearingTo(e.X, e.Y));

        if (GunHeat == 0)
            Fire(Math.Min(3.0, Math.Max(0.1, Energy - 0.2)));

        SetBack(140);

        moveDirection *= -1;
        strafeDirection *= -1;
        stuckCounter += 8;
    }

    public override void OnWonRound(WonRoundEvent e)
    {
        TurnRight(360);
    }
}