import { describe, it, expect } from '@jest/globals';
import { validateEmail } from '@/utils/validation';

describe('validateEmail', () => {
  describe('WhenTheValueIsEmpty', () => {
    it('ThenItReturnsAnEmailRequiredMessage', () => {
      expect(validateEmail('')).toBe('Email required');
    });
  });

  describe('WhenTheValueIsOnlyWhitespace', () => {
    it('ThenItReturnsAnEmailRequiredMessage', () => {
      expect(validateEmail('   ')).toBe('Email required');
    });
  });

  describe('WhenTheValueIsAValidEmail', () => {
    it('ThenItReturnsTrue', () => {
      expect(validateEmail('jane@example.com')).toBe(true);
    });
  });

  describe('WhenTheValueHasSurroundingWhitespace', () => {
    it('ThenItIsTrimmedBeforeValidationAndReturnsTrue', () => {
      expect(validateEmail('  jane@example.com  ')).toBe(true);
    });
  });

  describe('WhenTheValueIsMissingTheAtSymbol', () => {
    it('ThenItReturnsAnInvalidEmailMessage', () => {
      expect(validateEmail('jane.example.com')).toBe('Invalid email');
    });
  });

  describe('WhenTheValueIsMissingTheDomain', () => {
    it('ThenItReturnsAnInvalidEmailMessage', () => {
      expect(validateEmail('jane@example')).toBe('Invalid email');
    });
  });

  describe('WhenTheValueContainsSpaces', () => {
    it('ThenItReturnsAnInvalidEmailMessage', () => {
      expect(validateEmail('jane doe@example.com')).toBe('Invalid email');
    });
  });

  describe('WhenTheValueHasMultipleAtSymbols', () => {
    it('ThenItReturnsAnInvalidEmailMessage', () => {
      expect(validateEmail('jane@doe@example.com')).toBe('Invalid email');
    });
  });
});
