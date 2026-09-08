import { Appointment } from './appointment.model';
import { MedicalRecord } from './medical-record.model';
import { Prescription } from './prescription.model';


export interface DoctorPatientSummary
{
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    dateOfBirth?: string | null;
    gender: string;
    address: string;
    bloodGroup: string;
    medicalHistory: string;
}

export interface DoctorConsultation
{
    appointment: Appointment;
    patient: DoctorPatientSummary;
    medicalRecords: MedicalRecord[];
    previousConsultations: Appointment[];
    previousPrescriptions: Prescription[];
}