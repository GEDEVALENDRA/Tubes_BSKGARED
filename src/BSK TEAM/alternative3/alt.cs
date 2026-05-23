using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class GreedyWarBot : Bot
{
    private class EnemyInfo
    {
        public int Id;
        public double X, Y;
        public double Energy;
        public double Distance;
        public long LastSeen;
    }

    private readonly Dictionary<int, EnemyInfo> enemies = new();
    private EnemyInfo target;

    private int moveDirection = 1;
    private int tick = 0;
    private readonly Random random = new();

    static void Main(string[] args)
    {
        new GreedyWarBot().Start();
    }

    GreedyWarBot() : base(BotInfo.FromFile("alt.json")) { }

    public override void Run()
    {
        BodyColor = Color.Black;
        TurretColor = Color.DarkRed;
        RadarColor = Color.Yellow;
        BulletColor = Color.Orange;
        ScanColor = Color.Red;

        MaxSpeed = 8;
        SetTurnRadarRight(double.PositiveInfinity);

        while (IsRunning)
        {
            tick++;

            SelectBestTargetGreedy();
            DoMovement();
            DoRadar();

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (IsTeammate(e.ScannedBotId))
            return;

        double distance = DistanceTo(e.X, e.Y);

        enemies[e.ScannedBotId] = new EnemyInfo
        {
            Id = e.ScannedBotId,
            X = e.X,
            Y = e.Y,
            Energy = e.Energy,
            Distance = distance,
            LastSeen = tick
        };

        SelectBestTargetGreedy();

        if (target != null && target.Id == e.ScannedBotId)
        {
            AimAndFire(target);
        }
    }

    private void SelectBestTargetGreedy()
    {
        double bestScore = double.MinValue;
        EnemyInfo bestEnemy = null;

        foreach (var enemy in enemies.Values)
        {
            if (tick - enemy.LastSeen > 40)
                continue;

            double lowHpScore = 500 / Math.Max(enemy.Energy, 1);
            double distanceScore = 1000 / Math.Max(enemy.Distance, 1);
            double wallScore = IsNearWall(enemy.X, enemy.Y) ? 150 : 0;
            double finishScore = enemy.Energy < 25 ? 250 : 0;

            double score =
                lowHpScore +
                distanceScore +
                wallScore +
                finishScore;

            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }

        target = bestEnemy;
    }

    private void AimAndFire(EnemyInfo enemy)
    {
        double gunBearing = GunBearingTo(enemy.X, enemy.Y);
        SetTurnGunLeft(gunBearing);

        double absGunBearing = Math.Abs(gunBearing);

        if (GunHeat > 0 || absGunBearing > 8)
            return;

        double power;

        if (enemy.Distance < 100 && Energy > 30)
            power = 3;
        else if (enemy.Distance < 250)
            power = 2;
        else
            power = 1;

        if (enemy.Energy < 12)
            power = Math.Min(3, enemy.Energy / 4 + 0.1);

        if (Energy < 20)
            power = Math.Min(power, 1);

        SetFire(power);
    }

    private void DoMovement()
    {
        if (target == null)
        {
            SetTurnRight(45);
            SetForward(150 * moveDirection);
            return;
        }

        double bearing = BearingTo(target.X, target.Y);

        // Gerak perpendicular agar susah ditembak
        double turn = bearing + 90 - (20 * moveDirection);
        SetTurnLeft(turn);

        if (tick % 35 == 0 || NearWallSelf())
            moveDirection *= -1;

        SetForward(180 * moveDirection);
    }

    private void DoRadar()
    {
        if (target == null)
        {
            SetTurnRadarRight(double.PositiveInfinity);
            return;
        }

        double radarBearing = RadarBearingTo(target.X, target.Y);
        SetTurnRadarLeft(radarBearing * 2);
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        moveDirection *= -1;

        double bearing = CalcBearing(e.Bullet.Direction);
        SetTurnLeft(90 - bearing);
        SetForward(180 * moveDirection);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        moveDirection *= -1;
        SetBack(150);
        SetTurnRight(70);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        if (IsTeammate(e.VictimId))
            return;

        double bearing = BearingTo(e.X, e.Y);
        SetTurnLeft(bearing);

        double gunBearing = GunBearingTo(e.X, e.Y);
        SetTurnGunLeft(gunBearing);

        if (Energy > 20)
            SetFire(3);
        else
            SetFire(1);

        SetBack(80);
    }

    private bool IsNearWall(double x, double y)
    {
        double margin = 80;
        return x < margin ||
               y < margin ||
               x > ArenaWidth - margin ||
               y > ArenaHeight - margin;
    }

    private bool NearWallSelf()
    {
        double margin = 90;
        return X < margin ||
               Y < margin ||
               X > ArenaWidth - margin ||
               Y > ArenaHeight - margin;
    }
}