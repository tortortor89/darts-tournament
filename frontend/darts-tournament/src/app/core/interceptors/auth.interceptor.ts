import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const notificationService = inject(NotificationService);
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

      // Handle 403 - forbidden (insufficient permissions)
      if (error.status === 403) {
        notificationService.showError('Accès refusé. Vous n\'avez pas les permissions nécessaires.');
      }

      // Show error message to user (except for 401 and 403 which have specific handling)
      if (error.status !== 401 && error.status !== 403) {
        const message = notificationService.getErrorMessage(error);
        notificationService.showError(message);
      }

      return throwError(() => error);
    })
  );
};
