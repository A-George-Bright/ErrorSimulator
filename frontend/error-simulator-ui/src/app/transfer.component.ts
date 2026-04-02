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
        console.groupEnd();

        const res = this.normalizeError(err);
        this.response.set(res);
        this.loading.set(false);
        this.notify(res);
      }
    });
  }

  /** Converts any HttpErrorResponse into a TransferResponse.
   *  Priority: use the structured body the backend sent — only fall back
   *  to generic messages when the body is absent (network-level failure). */
  private normalizeError(err: HttpErrorResponse): TransferResponse {
    const body = err.error;

    // Backend returned a structured TransferResponse (408, 409, 500, 400 …)
    if (body && typeof body === 'object' && typeof body.status === 'string') {
      return body as TransferResponse;
    }

    // Pure network failure — no HTTP response at all
    if (err.status === 0) {
      return {
        success: false,
        status: 'FAILED',
        message: 'Unable to connect to server. Please check your network.',
        reference: '',
        timestamp: new Date().toISOString()
      };
    }

    // Fallback for unexpected non-structured bodies
    return {
      success: false,
      status: err.status === 408 ? 'TIMEOUT' : 'FAILED',
      message: err.status === 408
        ? 'Transaction failed due to timeout. Please try again.'
        : 'Database unavailable. Please try after some time.',
      reference: '',
      failureReason: err.message,
      timestamp: new Date().toISOString()
    };
  }

  /** All toast messages use res.message exactly as sent by the backend. */
  private notify(res: TransferResponse) {
    switch (res.status) {
      case 'SUCCESS':
        this.toast.success(`Transfer successful · ${res.reference}`);
        break;
      case 'DUPLICATE':
        this.toast.warning(res.message);
        break;
      case 'TIMEOUT':
        this.toast.error(res.message);
        break;
      case 'FAILED':
        this.toast.error(res.message);
        break;
      default:
        this.toast.error(res.message || 'An unexpected error occurred. Please try again.');
    }
  }
}
