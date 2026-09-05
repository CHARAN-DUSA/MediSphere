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
    id(id: any, arg1: string, consultationNotes: any): unknown;
    appointment: Appointment;
    patient: DoctorPatientSummary;
    medicalRecords: MedicalRecord[];
    previousConsultations: Appointment[];
    previousPrescriptions: Prescription[];
}