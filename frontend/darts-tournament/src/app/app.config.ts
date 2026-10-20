import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { FeatureService } from './core/services/feature.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Modules activés chargés avant le premier rendu (nav et guards en dépendent)
    {
      provide: APP_INITIALIZER,
      useFactory: (features: FeatureService) => () => features.load(),
      deps: [FeatureService],
      multi: true
    }
  ]
};
