import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { interval } from 'rxjs';
import { SimulateService } from './simulate';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {

  protected readonly title = signal('error-simulator-ui');

  cpu = 0;
  ram = 0;
  private intervalId: any;

  constructor(private sim: SimulateService) {}

  private refreshStats() {
    this.sim.stats().subscribe({
      next: (res: any) => {
        console.log('Stats response', res);
        this.cpu = Number(res?.cpu ?? 0);
        this.ram = Number(res?.ram ?? res?.ramAvailable ?? 0);
      },
      error: err => {
        console.error('Stats update failed', err);
      }
    });
  }

  ngOnInit() {
    this.refreshStats();

    this.intervalId = setInterval(() => {
      this.refreshStats();
    }, 1000);
  }

  ngOnDestroy() {
    clearInterval(this.intervalId);
  }

  run(action: string) {
    let request;

    switch (action) {
      case 'cpu':
        request = this.sim.cpu();
        break;
      case 'memory':
        request = this.sim.memory();
        break;
      case 'slow':
        request = this.sim.slow();
        break;
      case 'db':
        request = this.sim.db();
        break;
      case 'exception':
        request = this.sim.exception();
        break;
      default:
        console.error('Unknown action');
        return;
    }

    request.subscribe({
      next: (res: any) => {
        console.log('SUCCESS:', res);
        this.refreshStats();
      },
      error: (err: any) => console.error('ERROR:', err)
    });
  }
}