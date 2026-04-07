import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],

  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],

  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  theme = inject(ThemeService);
}