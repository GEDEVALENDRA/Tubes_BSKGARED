using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class RamFire : Bot
{
    int turnDirection = 1;

    const double SAFE_DISTANCE = 300;
    const double DISTANCE_TOLERANCE = 20;

    static void Main(string[] args)
    {
        new RamFire().Start();
    }

    RamFire() : base(BotInfo.FromFile("alt.json")) { }

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x99, 0x99, 0x99);
        TurretColor = Color.FromArgb(0x88, 0x88, 0x88);
        RadarColor = Color.FromArgb(0x66, 0x66, 0x66);
        ScanColor = Color.Yellow;

        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        MaxSpeed = 8;

        while (IsRunning)
        {
            TurnRadarRight(360);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double distance = DistanceTo(e.X, e.Y);

        // Arahkan gun ke musuh dulu
        double gunBearing = GunBearingTo(e.X, e.Y);
        TurnGunLeft(gunBearing);

        // Tembak keras kalau sudah cukup lurus
        if (GunHeat == 0 && Abs(GunBearingTo(e.X, e.Y)) < 10)
        {
            Fire(3);
        }

        // Movement stabil: hindar tapi tidak spin berlebihan
        if (distance < SAFE_DISTANCE - DISTANCE_TOLERANCE)
        {
            TurnRight(25 * turnDirection);
            Back(120);
        }
        else if (distance > SAFE_DISTANCE + DISTANCE_TOLERANCE)
        {
            TurnRight(20 * turnDirection);
            Forward(80);
        }
        else
        {
            TurnRight(15 * turnDirection);
            Forward(50);
        }

        Rescan();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        TurnGunLeft(GunBearingTo(e.X, e.Y));

        if (GunHeat == 0)
        {
            Fire(3);
        }

        Back(140);
        TurnRight(45 * turnDirection);

        turnDirection *= -1;

        Rescan();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        Back(120);
        TurnRight(90 * turnDirection);

        turnDirection *= -1;

        Rescan();
    }

    private double Abs(double value)
    {
        return value < 0 ? -value : value;
    }
}