import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { VitalsSignalrService } from './services/vitals-signalr.service';
import { PatientHistory } from './models/patient-history.model';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIcon, MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet,
    FormsModule,
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatDividerModule,
    MatTableModule,
    MatToolbarModule,
    MatFormFieldModule,
    MatInputModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'patient-monitoring-client';
  vitalsSignalrService = inject(VitalsSignalrService);
  http = inject(HttpClient);
  result!: string;
  messages: string[] = [];
  history: PatientHistory[] = [];
  prevPatientId: number | null = null;
  valueControl = new FormControl<number>(1);
  jsonResult: any | null = null;
  patients: any[] = [];
  displayedColumns: string[] = ['id', 'firstName', 'lastName', 'actions'];
  constructor() {
    this.http.get<any[]>('/api/patient').subscribe((data) => {
      this.patients = data;
    });

    this.vitalsSignalrService.onReceiveAlert((message: string) => {
      this.messages.push(message);
      console.log('Received alert: ', message);
    });
  }

  selectPatient(p: any) {
    this.messages = [];
    this.history = [];
    if (this.prevPatientId) {
      this.vitalsSignalrService.unsubscribeFromPatient(this.prevPatientId.toString());
    }
    this.prevPatientId = p.id;
    this.vitalsSignalrService.subscribeToPatient(p.id.toString());
    this.getCombinData(p.id);
  }

  getCombinData(id: number) {
    this.http.get<any>('/api/fullData?patientId=' + id).subscribe((data) => {
      this.jsonResult = data;
    });
  }

  getData() {
    this.http.get<string>('/api/vitals/current/' + this.prevPatientId).subscribe((data) => {
      this.result = data;
    });
  }

  getHistory() {
    this.http.get<PatientHistory[]>('/api/history/' + this.prevPatientId).subscribe((data) => {
      this.history = data;
    });
  }

  makeAnError() {
    this.http.get<string>('/api/vitals/makeError/1').subscribe((data) => {
      this.result = data;
    });
  }


}
