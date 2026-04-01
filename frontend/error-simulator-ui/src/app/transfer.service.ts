import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface TransferRequest {
  fromAccountNumber: string;
  toAccountNumber: string;
  amount: number;
}

export interface TransferResponse {
  success: boolean;
  status: 'SUCCESS' | 'DUPLICATE' | 'TIMEOUT' | 'FAILED';
  message: string;
  reference: string;
  failureReason?: string;
  timestamp: string;
}

@Injectable({ providedIn: 'root' })
export class TransferService {
  private baseUrl = 'http://localhost:5031/api/transfer';

  constructor(private http: HttpClient) {}

  transfer(request: TransferRequest): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(this.baseUrl, request).pipe(
      tap({
        next: (res) => console.log('[TransferService] 2xx response:', res),
        error: (err) => console.error('[TransferService] HTTP error:', err.status, err.statusText)
      })
    );
  }
}
