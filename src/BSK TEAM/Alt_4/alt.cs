using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// Justinian - Optimized Greedy Bot
// Fokus: melee war, target selection efisien, movement adaptif, tembakan hemat energi
public class Justinian : Bot
{
    private class EnemyInfo
    {
        public int Id;
        public double X;
        public double Y;
        public double Energy;
        public double PreviousEnergy;
        public double Distance;
        public double Direction;
        public double Speed;
        public long LastSeen;
        public bool RecentlyFired;
    }

    private readonly Dictionary<int, EnemyInfo> enemies = new();
    private EnemyInfo target;

    private long tick = 0;
    private int moveDirection = 1;
    private readonly Random random = new();

    private const double WALL_MARGIN = 95;
    private const double MIN_DISTANCE = 135;
    private const double IDEAL_DISTANCE = 220;
    private const double MAX_DISTANCE = 420;

    static void Main(string[] args)
    {
        new Justinian().Start();
    }

    Justinian() : base(BotInfo.FromFile("alt.json")) { }

    public override void Run()
    {
        BodyColor = Color.Black;
        TurretColor = Color.Red;
        RadarColor = Color.Yellow;
        BulletColor = Color.Orange;
        ScanColor = Color.Gray;
        TracksColor = Color.Green;
        GunColor = Color.White;

        MaxSpeed = 8;

        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        SetFireAssist(true);
        SetTurnRadarRight(double.PositiveInfinity);

        while (IsRunning)
        {
            tick++;

            RemoveOldEnemies();
            SelectBestTargetGreedy();

            DoRadar();
            DoMovement();

            if (target != null)
                AimAndFire(target);

            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (IsTeammate(e.ScannedBotId))
            return;

        double distance = DistanceTo(e.X, e.Y);

        double previousEnergy = e.Energy;
        bool recentlyFired = false;

        if (enemies.ContainsKey(e.ScannedBotId))
        {
            previousEnergy = enemies[e.ScannedBotId].Energy;
            double energyDrop = previousEnergy - e.Energy;

            // Drop 0.1 - 3.0 biasanya tanda musuh menembak
            recentlyFired = energyDrop > 0.09 && energyDrop <= 3.01;
        }

        enemies[e.ScannedBotId] = new EnemyInfo
        {
            Id = e.ScannedBotId,
            X = e.X,
            Y = e.Y,
            Energy = e.Energy,
            PreviousEnergy = previousEnergy,
            Distance = distance,
            Direction = e.Direction,
            Speed = e.Speed,
            LastSeen = tick,
            RecentlyFired = recentlyFired
        };

        SelectBestTargetGreedy();

        if (target != null && target.Id == e.ScannedBotId)
        {
            if (recentlyFired && distance < 450)
                moveDirection *= -1;

            AimAndFire(target);
        }
    }

    private void SelectBestTargetGreedy()
    {
        double bestScore = double.MinValue;
        EnemyInfo bestEnemy = null;

        foreach (EnemyInfo enemy in enemies.Values)
        {
            if (tick - enemy.LastSeen > 55)
                continue;

            double distanceScore = 1600 / Math.Max(enemy.Distance, 1);
            double lowEnergyScore = 900 / Math.Max(enemy.Energy, 1);

            double killBonus = enemy.Energy < 18 ? 450 : 0;
            double veryLowBonus = enemy.Energy < 8 ? 500 : 0;

            double closeCombatBonus = enemy.Distance < 260 ? 220 : 0;
            double tooFarPenalty = enemy.Distance > 520 ? -300 : 0;

            double wallBonus = IsNearWall(enemy.X, enemy.Y) ? 160 : 0;

            // Musuh lambat lebih mudah ditembak
            double easyHitBonus = Math.Max(0, 180 - Math.Abs(enemy.Speed) * 25);

            // Jangan terlalu nafsu target jauh saat energi sendiri rendah
            double energySafetyPenalty = Energy < 20 && enemy.Distance > 300 ? -350 : 0;

            // Greedy utama: pilih target dengan peluang kill dan hit tertinggi sekarang
            double score =
                distanceScore +
                lowEnergyScore +
                killBonus +
                veryLowBonus +
                closeCombatBonus +
                wallBonus +
                easyHitBonus +
                tooFarPenalty +
                energySafetyPenalty;

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
        double power = GetEfficientFirePower(enemy);

        if (power <= 0 || GunHeat > 0)
            return;

        double bulletSpeed = 20 - 3 * power;
        double time = enemy.Distance / bulletSpeed;

        // Predictive aiming
        double predictedX = enemy.X + Math.Sin(ToRad(enemy.Direction)) * enemy.Speed * time;
        double predictedY = enemy.Y + Math.Cos(ToRad(enemy.Direction)) * enemy.Speed * time;

        predictedX = Clamp(predictedX, WALL_MARGIN, ArenaWidth - WALL_MARGIN);
        predictedY = Clamp(predictedY, WALL_MARGIN, ArenaHeight - WALL_MARGIN);

        double gunTurn = GunBearingTo(predictedX, predictedY);
        SetTurnGunLeft(gunTurn);

        double absGunTurn = Math.Abs(gunTurn);

        // Semakin jauh musuh, semakin ketat toleransi aim
        double aimTolerance;
        if (enemy.Distance < 140)
            aimTolerance = 10;
        else if (enemy.Distance < 280)
            aimTolerance = 6;
        else
            aimTolerance = 3.5;

        // Jangan buang energi kalau aim belum cukup lurus
        if (absGunTurn <= aimTolerance)
            SetFire(power);
    }

    private double GetEfficientFirePower(EnemyInfo enemy)
    {
        if (Energy < 8)
            return 0;

        if (Energy < 16)
            return enemy.Distance < 180 ? 0.9 : 0.6;

        // Finishing: jangan pakai power 3 kalau musuh tinggal sedikit
        if (enemy.Energy < 5)
            return 0.8;

        if (enemy.Energy < 12 && enemy.Distance < 260)
            return 1.4;

        if (enemy.Distance < 90)
            return Energy > 25 ? 3.0 : 1.8;

        if (enemy.Distance < 170)
            return Energy > 22 ? 2.5 : 1.5;

        if (enemy.Distance < 320)
            return Energy > 18 ? 1.8 : 1.0;

        if (enemy.Distance < 520)
            return Energy > 35 ? 1.2 : 0.7;

        return 0.5;
    }

    private void DoMovement()
    {
        if (NearWallSelf())
        {
            EscapeWall();
            return;
        }

        if (target == null)
        {
            // Search mode: gerak ringan sambil radar scan
            SetTurnRight(25);
            SetForward(140 * moveDirection);

            if (tick % 45 == 0)
                moveDirection *= -1;

            return;
        }

        double distance = DistanceTo(target.X, target.Y);
        double bearing = BearingTo(target.X, target.Y);

        // Kalau jauh, kejar tapi jangan lurus total
        if (distance > MAX_DISTANCE)
        {
            double chaseAngle = bearing + 18 * moveDirection;
            SetTurnLeft(chaseAngle);
            SetForward(Math.Min(distance - IDEAL_DISTANCE, 260));
            return;
        }

        // Kalau terlalu dekat, mundur diagonal agar tidak jadi target mudah
        if (distance < MIN_DISTANCE)
        {
            double retreatAngle = bearing + 70 * moveDirection;
            SetTurnLeft(retreatAngle);
            SetBack(135);
            return;
        }

        // Orbit adaptif: jarak ideal sambil strafe
        double orbitOffset = 90;

        if (distance < IDEAL_DISTANCE)
            orbitOffset = 105;
        else if (distance > IDEAL_DISTANCE + 70)
            orbitOffset = 65;

        double randomWobble = (tick % 20 < 10) ? 12 : -12;
        double turn = bearing + (orbitOffset * moveDirection) + randomWobble;

        SetTurnLeft(turn);

        // Ubah arah saat musuh kemungkinan menembak / periodik
        if ((target.RecentlyFired && target.Distance < 420) ||
            tick % 32 == 0 ||
            random.NextDouble() < 0.025)
        {
            moveDirection *= -1;
        }

        double moveAmount = 155;

        if (distance > IDEAL_DISTANCE + 80)
            moveAmount = 190;

        if (distance < IDEAL_DISTANCE - 60)
            moveAmount = 120;

        SetForward(moveAmount * moveDirection);
    }

    private void DoRadar()
    {
        if (target == null)
        {
            SetTurnRadarRight(double.PositiveInfinity);
            return;
        }

        double radarTurn = RadarBearingTo(target.X, target.Y);

        // Radar lock lebih kuat agar target tidak hilang
        SetTurnRadarLeft(radarTurn * 2.2);
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        moveDirection *= -1;

        double escapeAngle = NormalizeRelativeAngle((e.Bullet.Direction + 90) - Direction);

        SetTurnLeft(escapeAngle);
        SetForward(180 * moveDirection);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        if (IsTeammate(e.VictimId))
            return;

        double gunTurn = GunBearingTo(e.X, e.Y);
        SetTurnGunLeft(gunTurn);

        if (GunHeat == 0)
        {
            if (Energy > 25)
                SetFire(3);
            else
                SetFire(1.2);
        }

        moveDirection *= -1;
        SetBack(100);
        SetTurnRight(55);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        EscapeWall();
    }

    public override void OnBulletHit(BulletHitBotEvent e)
    {
        if (enemies.ContainsKey(e.VictimId))
            target = enemies[e.VictimId];
    }

    public override void OnBotDeath(BotDeathEvent e)
    {
        if (enemies.ContainsKey(e.VictimId))
            enemies.Remove(e.VictimId);

        if (target != null && target.Id == e.VictimId)
            target = null;
    }

    private void EscapeWall()
    {
        moveDirection *= -1;

        double centerBearing = BearingTo(ArenaWidth / 2, ArenaHeight / 2);

        SetTurnLeft(centerBearing);
        SetForward(220);
    }

    private void RemoveOldEnemies()
    {
        List<int> removeList = new();

        foreach (var pair in enemies)
        {
            if (tick - pair.Value.LastSeen > 90)
                removeList.Add(pair.Key);
        }

        foreach (int id in removeList)
            enemies.Remove(id);
    }

    private bool IsNearWall(double x, double y)
    {
        return x < WALL_MARGIN ||
               y < WALL_MARGIN ||
               x > ArenaWidth - WALL_MARGIN ||
               y > ArenaHeight - WALL_MARGIN;
    }

    private bool NearWallSelf()
    {
        return X < WALL_MARGIN ||
               Y < WALL_MARGIN ||
               X > ArenaWidth - WALL_MARGIN ||
               Y > ArenaHeight - WALL_MARGIN;
    }

    private double NormalizeRelativeAngle(double angle)
    {
        while (angle > 180)
            angle -= 360;

        while (angle < -180)
            angle += 360;

        return angle;
    }

    private double ToRad(double degree)
    {
        return degree * Math.PI / 180.0;
    }

    private double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}