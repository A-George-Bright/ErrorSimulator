import { Component, signal, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SimulateService } from './simulate';

@Component({
  selector: 'app-simulate',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './simulate.component.html',
  styleUrl: './simulate.component.css'

})
export class SimulateComponent implements OnInit, OnDestroy {
  private sim = inject(SimulateService);

  cpu = signal(0);
  ram = signal(0);
  totalRam = signal(0);
  memoryPercent = signal(0);
  connectionStatus = signal<'connected' | 'disconnected' | 'demo'>('disconnected');
  statusMessage = signal('Connecting...');
  loading = signal(new Set<string>());
  private intervalId: any;

  isLoading = (action: string) => this.loading().has(action);

  refreshStats() {
    this.sim.stats().subscribe({
      next: (res: any) => {
        this.cpu.set(Number(res?.systemCpu ?? 0));
        this.ram.set(Number(res?.usedMemoryMb ?? 0));
        this.totalRam.set(Number(res?.totalMemoryMb ?? 0));
        this.memoryPercent.set(Number(res?.memoryLoadPercent ?? 0));
        this.connectionStatus.set('connected');
        this.statusMessage.set('Live Stats Streaming');
      },
      error: () => {
        this.cpu.set(0);
        this.ram.set(0);
        this.connectionStatus.set('demo');
        this.statusMessage.set('Backend Offline - Demo Mode');
      }
    });
  }

  ngOnInit() {
    this.refreshStats();
    this.intervalId = setInterval(() => this.refreshStats(), 1000);
  }

  ngOnDestroy() {
    clearInterval(this.intervalId);
  }

  run(action: string) {
    console.log('CLICKED 👉', action);
    const loadingSet = new Set(this.loading());
    loadingSet.add(action);
    this.loading.set(loadingSet);

    const request = this.getRequest(action);
    request.subscribe({
      next: () => this.refreshStats(),
      error: () => { },
      complete: () => {
        loadingSet.delete(action);
        this.loading.set(loadingSet);
      }
    });
  }

  private getRequest(action: string) {
    switch (action) {
      case 'cpu': return this.sim.cpu();
      case 'cpuReduce': return this.sim.cpuReduce();

      case 'memoryStart': return this.sim.memoryStart();
      case 'memoryStop': return this.sim.memoryStop();

      case 'slow': return this.sim.slow();
      case 'slowRandom': return this.sim.slowRandom();

      case 'dbDown': return this.sim.dbDown();
      case 'dbTimeout': return this.sim.dbTimeout();
      case 'dbIntermittent': return this.sim.dbIntermittent();
      case 'dbReset': return this.sim.dbReset();

      case 'exception': return this.sim.exception();
      case 'stack': return this.sim.stack();

      case 'cpuStop': return this.sim.cpuStop();
      case 'stopAll': return this.sim.stopAll();

      default: throw new Error('Unknown action');
    }

  }
}
