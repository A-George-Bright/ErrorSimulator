import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SimulateService } from './simulate';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('error-simulator-ui');
  constructor(private sim: SimulateService) {}

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
