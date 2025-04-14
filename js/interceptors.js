// constants.ts
export const DOMAIN_API_MAP: Record<string, string> = {
  'localhost:4200': 'https://dev-api.example.com',
  'app.example.com': 'https://api.example.com',
  'test.example.com': 'https://test-api.example.com',
  // add more as needed
};






// api-domain.interceptor.ts
import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { DOMAIN_API_MAP } from './constants';

@Injectable()
export class ApiDomainInterceptor implements HttpInterceptor {
  private apiBaseUrl: string;

  constructor() {
    const currentHost = window.location.host;
    this.apiBaseUrl = DOMAIN_API_MAP[currentHost] || '';
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip if full URL is already used
    if (req.url.startsWith('http')) {
      return next.handle(req);
    }

    const updatedReq = req.clone({
      url: this.apiBaseUrl + req.url
    });

    return next.handle(updatedReq);
  }
}




// app.module.ts
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ApiDomainInterceptor } from './api-domain.interceptor';

@NgModule({
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ApiDomainInterceptor,
      multi: true
    }
  ]
})
export class AppModule {}