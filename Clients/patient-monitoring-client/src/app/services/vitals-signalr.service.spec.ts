import { TestBed } from '@angular/core/testing';

import { VitalsSignalrService } from './vitals-signalr.service';

describe('VitalsSignalrService', () => {
  let service: VitalsSignalrService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(VitalsSignalrService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
