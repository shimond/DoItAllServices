export type VitalsData = {
  temperature: number;
  bloodPressure: {
    systolic: number;
    diastolic: number;
  };
  heartRate: number;
  respiratoryRate: number;
  oxygenSaturation: number;
};

export type PatientHistory = {
  id: number;
  patientId: string;
  vitalsDataJson: string;
  recordedAt: string;
};