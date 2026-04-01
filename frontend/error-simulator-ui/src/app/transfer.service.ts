import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface TransferRequest {
  transactionId: string;
  fromAccountId: number;
  toAccountId: number;
  amount: number;
}

export interface TransferResponse {
  status: 'SUCCESS' | 'DUPLICATE' | 'TIMEOUT' | 'FAILED';
  message: string;
  balance?: number;
}

@Injectable({
  providedIn: 'root'
})
export class TransferService {
  private baseUrls = ['https://localhost:7290/api/transfer', 'http://localhost:5031/api/transfer'];

  constructor(private http: HttpClient) { }

  transfer(request: TransferRequest): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(this.baseUrls[0], request);
  }
}
