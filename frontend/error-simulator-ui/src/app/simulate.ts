import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SimulateService {

  baseUrls = ['https://localhost:7290/api/simulate', 'http://localhost:5031/api/simulate'];
  currentUrl = this.baseUrls[0];

  constructor(private http: HttpClient) { }

  private tryUrl<T>(path: string) {
    const url = `${this.currentUrl}/${path}`;
    return this.http.get<T>(url).pipe(
      catchError(err => {
        console.warn(`URL ${url} failed, trying fallback`, err);
        this.currentUrl = this.baseUrls.find(u => u !== this.currentUrl) ?? this.currentUrl;
        const fallback = `${this.currentUrl}/${path}`;
        return this.http.get<T>(fallback);
      })
    );
  }

  private tryPost<T>(path: string) {
    const url = `${this.currentUrl}/${path}`;
    return this.http.post<T>(url, {}).pipe(
      catchError(err => {
        console.warn(`URL ${url} failed, trying fallback`, err);
        this.currentUrl = this.baseUrls.find(u => u !== this.currentUrl) ?? this.currentUrl;
        const fallback = `${this.currentUrl}/${path}`;
        return this.http.post<T>(fallback, {});
      })
    );
  }

  cpu() {
    return this.tryPost('cpu');
  }

  cpuStop() {
    return this.tryUrl('cpu/stop');
  }

  stopAll() {
    return this.tryUrl('stop-all');
  }

  slow() {
    return this.tryPost('slow');
  }

  db() {
    return this.tryPost('db');
  }

  exception() {
    return this.tryPost('exception');
  }

  stats() {
    return this.tryUrl<any>('stats');
  }

  memoryStart() {
    return this.tryUrl('memory/start');
  }

  memoryStop() {
    return this.tryUrl('memory/stop');
  }
}