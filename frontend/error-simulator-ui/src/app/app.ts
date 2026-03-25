import { Component, signal } from '@angular/core';

import { SimulateService } from './simulate';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {

  protected readonly title = signal('error-simulator-ui');

  cpu = 0;
  ram = 0;

  constructor(private sim: SimulateService) {}

  // ✅ MOVE IT HERE (inside class)
  ngOnInit() {
    setInterval(() => {
      this.sim.stats().subscribe((res: any) => {
        this.cpu = res.cpu;
        this.ram = res.ram;
      });
    }, 3000);
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
      next: (res: any) => console.log('SUCCESS:', res),
      error: (err: any) => console.error('ERROR:', err)
    });
  }
}