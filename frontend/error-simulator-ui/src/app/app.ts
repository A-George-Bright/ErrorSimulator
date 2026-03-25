import { Component, signal, OnInit, OnDestroy } from '@angular/core';
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

  cpu = signal(0);
  ram = signal(0);
  connectionStatus = signal<'connected' | 'disconnected'>('disconnected');
  statusMessage = signal('Connecting...');
  private intervalId: any;

  constructor(private sim: SimulateService) { }

  refreshStats() {
    this.sim.stats().subscribe({
      next: (res: any) => {
        console.log('Stats response', res);
        this.cpu.set(Number(res?.systemCpu ?? res?.cpu ?? 0));
        this.ram.set(Number(res?.ramAvailableMb ?? res?.ram ?? res?.ramAvailable ?? 0));
        this.connectionStatus.set('connected');
        this.statusMessage.set('Live data streaming');
      },
      error: err => {
        console.error('Stats update failed', err);
        this.connectionStatus.set('disconnected');
        this.statusMessage.set('Cannot reach API; retrying...');
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
      case 'memoryStart':
        request = this.sim.memoryStart();
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
      case 'cpuStop':
        request = this.sim.cpuStop();
        break;
      case 'memoryStop':
        request = this.sim.memoryStop();
        break;
      case 'stopAll':
        request = this.sim.stopAll();
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