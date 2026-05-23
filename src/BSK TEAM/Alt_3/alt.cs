using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class EvasiveCircleBot : Bot
{
    int moveDir = 1;
    int turnDir = 1;

    static void Main(string[] args)
    {
        new EvasiveCircleBot().Start();
    }

    EvasiveCircleBot() : base(BotInfo.FromFile("EvasiveCircleBot.json")) { }

    public override void Run()
    {
        BodyColor = Color.DarkBlue;
        TurretColor = Color.Cyan;
        RadarColor = Color.Yellow;
        BulletColor = Color.White;

        MaxSpeed = 7;
        SetTurnRadarRight(Double.PositiveInfinity);

        while (IsRunning)
        {
            SetTurnRight(35 * turnDir);
            SetForward(250 * moveDir);
            SetTurnGunRight(25);
            Go();
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (IsTeammate(e.ScannedBotId)) return;

        double distance = DistanceTo(e.X, e.Y);
        double gunBearing = GunBearingTo(e.X, e.Y);

        SetTurnGunLeft(gunBearing);

        if (Math.Abs(gunBearing) < 8 && GunHeat == 0)
        {
            if (distance < 120 && Energy > 30)
                Fire(3);
            else if (distance < 350)
                Fire(2);
            else
                Fire(1);
        }

        if (distance < 180)
        {
            moveDir *= -1;
            SetBack(180);
        }

        Rescan();
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        moveDir *= -1;
        turnDir *= -1;

        SetTurnRight(70 * turnDir);
        SetForward(220 * moveDir);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        moveDir *= -1;
        SetBack(180);
    }

    public override void OnHitBot(HitBotEvent e)
    {
        if (e.IsRammed)
        {
            moveDir *= -1;
            SetBack(120);
        }

        double gunBearing = GunBearingTo(e.X, e.Y);
        TurnGunLeft(gunBearing);

        if (GunHeat == 0)
            Fire(3);
    }
}