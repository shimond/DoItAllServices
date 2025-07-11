namespace AlertingService.Models;

public class VitalsData
{
    public double Temperature { get; set; } // In Celsius
    public BloodPressure BloodPressure { get; set; }
    public int HeartRate { get; set; } // Beats per minute
    public int RespiratoryRate { get; set; } // Breaths per minute
    public int OxygenSaturation { get; set; } // SpO2 percentage
}

public class BloodPressure
{
    public int Systolic { get; set; } // Top number (mmHg)
    public int Diastolic { get; set; } // Bottom number (mmHg)

    public override string ToString()
    {
        return $"{Systolic}/{Diastolic}";
    }
}