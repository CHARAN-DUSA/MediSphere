import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) { auth.logout(); }
      else if (err.status === 403) { toast.error('Access denied.'); }
      else if (err.status === 429) { toast.error('Too many requests. Please slow down.'); }
      else { toast.error(err.error?.message ?? 'An error occurred.'); }
      return throwError(() => err);
    })
  );
};
