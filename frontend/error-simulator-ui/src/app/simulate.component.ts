import {
  Component, signal, OnInit, OnDestroy, inject,
  ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { SimulateService } from './simulate';
import {
  interval, Subject, switchMap, takeUntil,
  distinctUntilChanged, startWith, catchError, of
} from 'rxjs';

@Component({
  selector: 'app-simulate',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './simulate.component.html',
  styleUrl: './simulate.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SimulateComponent implements OnInit, OnDestroy {
  private sim = inject(SimulateService);
  private destroy$ = new Subject<void>();

  // Signals exposed to template — identical names, no template changes needed
  cpu = signal(0);
  ram = signal(0);
  totalRam = signal(0);
  memoryPercent = signal(0);
  connectionStatus = signal<'connected' | 'disconnected' | 'demo'>('disconnected');
  statusMessage = signal('Connecting...');
  loading = signal(new Set<string>());

  // Tracks whether we have ever received a successful response.
  // Used to distinguish "still connecting" from "was connected, now offline".
  private hasEverConnected = false;

  isLoading = (action: string) => this.loading().has(action);

  ngOnInit() {
    interval(1000).pipe(
      startWith(0),            // fire immediately on init, no 1s wait
      switchMap(() =>          // cancel in-flight request before starting next
        this.sim.stats().pipe(
          catchError(() => of(null))  // absorb errors so the poll stream stays alive
        )
      ),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      takeUntil(this.destroy$)
    ).subscribe(res => {
      if (res !== null) {
        // Successful poll — update all metrics and mark connected
        this.hasEverConnected = true;
        this.cpu.set(Number(res?.systemCpu ?? 0));
        this.ram.set(Number(res?.usedMemoryMb ?? 0));
        this.totalRam.set(Number(res?.totalMemoryMb ?? 0));
        this.memoryPercent.set(Number(res?.memoryLoadPercent ?? 0));
        this.connectionStatus.set('connected');
        this.statusMessage.set('Live Stats Streaming');
      } else {
        // Failed poll — only update status indicator, never reset metric values.
        // This prevents the 0% / offline flicker on transient failures.
        if (this.hasEverConnected) {
          this.connectionStatus.set('demo');
          this.statusMessage.set('Backend Offline - Demo Mode');
        } else {
          this.connectionStatus.set('disconnected');
          this.statusMessage.set('Connecting...');
        }
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  run(action: string) {
    console.log('CLICKED 👉', action);
    const loadingSet = new Set(this.loading());
    loadingSet.add(action);
    this.loading.set(loadingSet);

    this.getRequest(action).subscribe({
      next: () => { },
      error: () => { },
      complete: () => {
        const s = new Set(this.loading());
        s.delete(action);
        this.loading.set(s);
      }
    });
  }

  private getRequest(action: string) {
    switch (action) {
      case 'cpu':            return this.sim.cpu();
      case 'cpuReduce':      return this.sim.cpuReduce();
      case 'memoryStart':    return this.sim.memoryStart();
      case 'memoryStop':     return this.sim.memoryStop();
      case 'slow':           return this.sim.slow();
      case 'slowRandom':     return this.sim.slowRandom();
      case 'dbDown':         return this.sim.dbDown();
      case 'dbTimeout':      return this.sim.dbTimeout();
      case 'dbIntermittent': return this.sim.dbIntermittent();
      case 'dbReset':        return this.sim.dbReset();
      case 'exception':      return this.sim.exception();
      case 'stack':          return this.sim.stack();
      case 'cpuStop':        return this.sim.cpuStop();
      case 'stopAll':        return this.sim.stopAll();
      default:               throw new Error('Unknown action');
    }
  }
}
