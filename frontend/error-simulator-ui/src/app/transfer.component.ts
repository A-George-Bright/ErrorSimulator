import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TransferService, TransferRequest, TransferResponse } from './transfer.service';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-transfer',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './transfer.component.html',
  styleUrls: ['./transfer.component.css']
})
export class TransferComponent {
  request = signal<TransferRequest>({
    fromAccountNumber: 'ACC-000001',
    toAccountNumber: 'ACC-000002',
    amount: 100
  });

  protected response = signal<TransferResponse | null>(null);
  protected loading = signal(false);

  constructor(
    private transferService: TransferService,
    private toast: ToastService
  ) {}

  simulateTransfer() {
    const req = this.request();
    this.loading.set(true);
    this.response.set(null);

    console.group('[Transfer] Outgoing request');
    console.log('Payload:', { ...req });
    console.log('Endpoint: POST http://localhost:5000/api/transfer');
    console.groupEnd();

    this.transferService.transfer(req).subscribe({
      next: (res) => {
        console.group('[Transfer] Response ← 200 OK');
        console.log('Body:', res);
        console.groupEnd();

        this.response.set(res);
        this.loading.set(false);
        this.notify(res);
      },
      error: (err: HttpErrorResponse) => {
        console.group(`[Transfer] Error ← HTTP ${err.status}`);
        console.error('HttpErrorResponse:', err);
        console.log('Body:', err.error);
        console.groupEnd();

        const res = this.normalizeError(err);
        this.response.set(res);
        this.loading.set(false);
        this.notify(res);
      }
    });
  }

  /** Converts any HttpErrorResponse into a well-typed TransferResponse. */
  private normalizeError(err: HttpErrorResponse): TransferResponse {
    const body = err.error;

    // Backend returned a structured TransferResponse body (409, 408, 500)
    if (body && typeof body === 'object' && typeof body.status === 'string') {
      return body as TransferResponse;
    }

    // Map HTTP status codes to known statuses when the body isn't structured
    let status: TransferResponse['status'] = 'FAILED';
    let message = 'An unexpected error occurred — please try again';

    if (err.status === 0) {
      message = 'Network error — could not reach server. Is the backend running?';
    } else if (err.status === 408) {
      status = 'TIMEOUT';
      message = 'Request timed out — please retry';
    } else if (err.status === 409) {
      status = 'DUPLICATE';
      message = 'Duplicate transaction — already processed';
    } else if (err.status >= 500) {
      message = `Server error (${err.status}) — please try again`;
    } else if (err.status >= 400) {
      message = `Bad request (${err.status}) — check your input`;
    }

    return {
      success: false,
      status,
      message,
      reference: '',
      failureReason: err.message,
      timestamp: new Date().toISOString()
    };
  }

  private notify(res: TransferResponse) {
    switch (res.status) {
      case 'SUCCESS':
        this.toast.success(`Transfer successful · ${res.reference}`);
        break;
      case 'DUPLICATE':
        this.toast.warning(`Duplicate transaction — already processed`);
        break;
      case 'TIMEOUT':
        this.toast.warning(`Request timed out · ${res.failureReason ?? 'please retry'}`);
        break;
      case 'FAILED':
        this.toast.error(`Transfer failed · ${res.failureReason ?? res.message}`);
        break;
      default:
        this.toast.error(`Unknown error — ${res.message || 'please retry'}`);
        console.warn('[Transfer] Unhandled status:', res.status, res);
    }
  }
}
