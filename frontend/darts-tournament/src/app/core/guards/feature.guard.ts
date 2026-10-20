import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { FeatureService } from '../services/feature.service';
import { FeatureFlags } from '../models';

export const featureGuard = (feature: keyof FeatureFlags): CanActivateFn => () => {
  if (inject(FeatureService).isEnabled(feature)) {
    return true;
  }

  return inject(Router).createUrlTree(['/tournaments']);
};
