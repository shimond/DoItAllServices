import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatOptionModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-add-patient',
  templateUrl: './add-patient.component.html',
  standalone:true,
  imports:[
      MatCardModule,
      MatFormFieldModule,
      
      MatInputModule,
      MatButtonModule,
      MatSelectModule,
    ReactiveFormsModule, 
    FormsModule,
    CommonModule,
  ],
  styleUrls: ['./add-patient.component.scss']
})
export class AddPatientComponent {
  patientForm: FormGroup;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

  constructor(private fb: FormBuilder, private http: HttpClient) {
    this.patientForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required],
      address: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.patientForm.invalid) return;
    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';
    const patient = this.patientForm.value;
    this.http.post('/api/patient', patient).subscribe({
      next: () => {
        this.successMessage = 'Patient added successfully!';
        this.patientForm.reset();
        this.isSubmitting = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to add patient.';
        this.isSubmitting = false;
      }
    });
  }
}
