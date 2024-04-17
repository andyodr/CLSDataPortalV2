import { Injectable } from "@angular/core"
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
  HttpClient
} from "@angular/common/http"
import { catchError, Observable } from 'rxjs';
import { NavigationExtras, Router } from '@angular/router';
import { LoggerService } from '../_services/logger.service';
import { environment } from "src/environments/environment";

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private router: Router, private http: HttpClient, private logger: LoggerService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error) {
          switch (error.status) {
            case 400:
              if (error.error.errors) {
                const modalStateErrors = [];
                for (const key in error.error.errors) {
                  if (error.error.errors[key]) {
                    modalStateErrors.push(error.error.errors[key]);
                  }
                }
                throw modalStateErrors.flat();
              }
              else {
                this.logger.logError(error.error + error.status.toString());
              }

              break
            case 401:
            case 403:
              this.logger.logError("Unauthorised " + error.status.toString())
              this.http.get(environment.baseUrl + "api/SignOut", { observe: "response" })
                .subscribe({
                    next: result => {
                        if (result.status == 200) {
                            localStorage.removeItem("userState")
                            this.router.navigateByUrl("/")
                        }
                    }
                })
                break
            case 404:
              this.router.navigateByUrl('/not-found');
              break;
            case 500:
              const navigationExtras: NavigationExtras = {state: {error: error.error}};
              this.router.navigateByUrl('/server-error', navigationExtras);
              break;
            default:
              console.error(error);
              break;
          }
        }
        throw error;
      })
    )
  }
}
