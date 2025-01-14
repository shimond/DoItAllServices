import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class VitalsSignalrService {
  private hubConnection!: signalR.HubConnection;

  constructor() {
    this.startConnection();
  }

  private startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`/api/vitalsHub`) // Use the BFF URL to forward to the Alerting Service
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR connection established.');
      })
      .catch((err:any) => console.error('Error establishing SignalR connection: ', err));
  }

  public subscribeToPatient(patientId: string): void {
    this.hubConnection.invoke('SubscribeToPatient', patientId)
      .catch((err:any) => console.error('Error subscribing to patient: ', err));
  }

  public unsubscribeFromPatient(patientId: string): void {
    this.hubConnection.invoke('UnsubscribeFromPatient', patientId)
      .catch((err:any) => console.error('Error unsubscribing from patient: ', err));
  }

  public onReceiveAlert(callback: (message: string) => void): void {
    this.hubConnection.on('ReceiveAlert', (message: string) => {
      callback(message);
    });
  }
}
