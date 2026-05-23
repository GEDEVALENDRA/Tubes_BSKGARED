using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class RamFire : Bot
{
    int moveDirection = 1;
    int strafeDirection = 1;

    double lastEnemyEnergy = 100;

    const double WALL_MARGIN = 90;
    const double DANGER_MARGIN = 130;

    static void Main(string[] args)
    {
        new RamFire().Start();
    }

    RamFire() : base(BotInfo.FromFile("main.json")) { }

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

        while (IsRunning)
        {
            SetTurnRadarRight(360);

            AvoidWallRoute();

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double distance = DistanceTo(e.X, e.Y);
        double absoluteBearing = DirectionTo(e.X, e.Y);

        double radarTurn = NormalizeRelativeAngle(absoluteBearing - RadarDirection);
        SetTurnRadarLeft(radarTurn * 2.5);

        double energyDrop = lastEnemyEnergy - e.Energy;

        if (energyDrop > 0.09 && energyDrop <= 3.01)
        {
            moveDirection *= -1;
            strafeDirection *= -1;

            SetForward(190 * moveDirection);
        }

        lastEnemyEnergy = e.Energy;

        if (IsNearWall() || WillHitWall(170))
        {
            MoveToSafeRoute();
        }
        else
        {
            double orbitAngle = 90 - (22 * moveDirection);

            double bodyTurn =
                NormalizeRelativeAngle(
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

        double gunTurn =
            NormalizeRelativeAngle(
                (absoluteBearing - GunDirection) + aimOffset
            );

        SetTurnGunLeft(gunTurn);

        if (GunHeat == 0 && Math.Abs(gunTurn) < GetAimTolerance(distance))
        {
            double power = GetFirePower(distance);

            Fire(Math.Min(power, Energy - 0.2));
        }

        Rescan();
    }

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

        double nextX =
            X + Math.Sin(rad) * distanceAhead * moveDirection;

        double nextY =
            Y + Math.Cos(rad) * distanceAhead * moveDirection;

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

        double turn =
            NormalizeRelativeAngle(
                safeDirection - Direction
            );

        moveDirection = 1;

        if (Math.Abs(turn) > 90)
        {
            turn =
                NormalizeRelativeAngle(turn + 180);

            moveDirection = -1;
        }

        strafeDirection *= -1;

        SetTurnLeft(turn);
        SetForward(220 * moveDirection);
    }

    double GetFirePower(double distance)
    {
        if (Energy < 15)
            return 1.2;

        if (distance < 90)
            return 3.0;

        if (distance < 180)
            return 2.7;

        if (distance < 350)
            return 2.1;

        return 1.4;
    }

    double GetAimTolerance(double distance)
    {
        if (distance < 120)
            return 14;

        if (distance < 250)
            return 9;

        if (distance < 450)
            return 5;

        return 3;
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        moveDirection *= -1;
        strafeDirection *= -1;

        TurnLeft(
            NormalizeRelativeAngle(
                90 - (Direction - e.Bullet.Direction)
            )
        );

        SetForward(220 * moveDirection);
    }

    // ===== HIT WALL =====

    public override void OnHitWall(HitWallEvent e)
    {
        moveDirection *= -1;
        strafeDirection *= -1;

        SetBack(180);
        SetTurnRight(100);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        TurnLeft(BearingTo(e.X, e.Y));

        TurnGunLeft(GunBearingTo(e.X, e.Y));

        if (GunHeat == 0)
            Fire(3);

        SetBack(100);

        moveDirection *= -3;
        strafeDirection *= -3;
    }

    public override void OnWonRound(WonRoundEvent e)
    {
        TurnRight(360);
    }
}