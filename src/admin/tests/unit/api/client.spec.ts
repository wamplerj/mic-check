import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { setAuthTokens, clearAuthTokens, getAccessToken } from '@/api/client';

// The interceptor internals rely on axios, which is hard to unit-test without
// a real HTTP stack. These tests focus on the exported token-management helpers
// that the interceptors and auth store both depend on.

describe('ApiClient token management', () => {
  beforeEach(() => {
    clearAuthTokens();
    localStorage.clear();
  });

  afterEach(() => {
    clearAuthTokens();
  });

  describe('WhenTokensAreSet', () => {
    it('ThenGetAccessTokenReturnsTheAccessToken', () => {
      setAuthTokens('access-abc', 'refresh-xyz');

      expect(getAccessToken()).toBe('access-abc');
    });
  });

  describe('WhenTokensAreCleared', () => {
    it('ThenGetAccessTokenReturnsNull', () => {
      setAuthTokens('access-abc', 'refresh-xyz');
      clearAuthTokens();

      expect(getAccessToken()).toBeNull();
    });
  });

  describe('WhenSetAuthTokensIsCalledWithNull', () => {
    it('ThenGetAccessTokenReturnsNull', () => {
      setAuthTokens(null, null);

      expect(getAccessToken()).toBeNull();
    });
  });

  describe('WhenTokensAreUpdated', () => {
    it('ThenGetAccessTokenReturnsTheLatestToken', () => {
      setAuthTokens('first-token', 'first-refresh');
      setAuthTokens('second-token', 'second-refresh');

      expect(getAccessToken()).toBe('second-token');
    });
  });
});
