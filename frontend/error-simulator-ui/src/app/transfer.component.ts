import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TransferService, TransferRequest, TransferResponse } from './transfer.service';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-transfer',
  standalone: true,
  imports: [CommonModule, FormsModule],

  templateUrl: `./transfer.component.html`,

  styleUrls: ['./transfer.component.css']
})
export class TransferComponent {
  request = signal<TransferRequest>({
    transactionId: '',
    fromAccountId: 1,
    toAccountId: 2,
    amount: 100
  });

  
  protected response = signal<TransferResponse | null>(null);
  protected loading = signal(false);
  
  constructor(private transferService: TransferService) {}
  
  simulateTransfer() {
    this.loading.set(true);
    this.response.set(null);
    
    this.transferService.transfer(this.request()).subscribe({

      next: (res) => {
        this.response.set(res);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Transfer error', err);
        this.response.set({
          status: 'FAILED',
          message: 'Network error'
        });
        this.loading.set(false);
      }
    });
  }
}
