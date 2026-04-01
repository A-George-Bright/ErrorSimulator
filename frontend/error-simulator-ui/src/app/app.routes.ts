import { Routes } from '@angular/router';

import { SimulateComponent } from './simulate.component';
import { TransferComponent } from './transfer.component';

export const routes: Routes = [
  { path: '', redirectTo: '/simulate', pathMatch: 'full' },
  { path: 'simulate', component: SimulateComponent },
  { path: 'transfer', component: TransferComponent }
];
