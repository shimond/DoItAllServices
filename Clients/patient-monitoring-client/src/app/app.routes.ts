
import { Routes } from '@angular/router';
import { AddPatientComponent } from './add-patient/add-patient.component';
import { DashboardComponent } from './dashboard/dashboard.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'add-patient', component: AddPatientComponent },
];
