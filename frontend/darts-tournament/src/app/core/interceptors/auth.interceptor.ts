import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ErrorService } from '../services/error.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const errorService = inject(ErrorService);
  const router = inject(Router);
  const token = authService.getToken();

  let request = req;
  if (token) {
    request = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
  }

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 - redirect to login
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }

      // Show error message to user (except for 401 which redirects)
      if (error.status !== 401) {
        const message = errorService.getErrorMessage(error);
        errorService.showError(message, error.error?.code);
      }

      return throwError(() => error);
    })
  );
};
