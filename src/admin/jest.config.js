/** @type {import('jest').Config} */
module.exports = {
  testEnvironment: 'jsdom',
  testEnvironmentOptions: {
    customExportConditions: ['node', 'node-addons'],
  },
  roots: ['<rootDir>/src', '<rootDir>/tests'],
  testMatch: [
    '**/__tests__/**/*.spec.[jt]s?(x)',
    '**/*.spec.[jt]s?(x)',
    '**/*.test.[jt]s?(x)',
  ],
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'vue', 'json'],
  transform: {
    '^.+\\.vue$': '@vue/vue3-jest',
    '^.+\\.[jt]sx?$': 'babel-jest',
  },
  transformIgnorePatterns: [
    '/node_modules/(?!(vuetify|@vueuse|@iconify)/)',
  ],
  moduleNameMapper: {
    '\\.(css|scss|sass)$': '<rootDir>/tests/__mocks__/styleMock.js',
    '\\.(svg|png|jpg|jpeg|gif|webp|woff2?)$': '<rootDir>/tests/__mocks__/fileMock.js',
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@core/(.*)$': '<rootDir>/src/@core/$1',
    '^@core$': '<rootDir>/src/@core',
    '^@layouts/(.*)$': '<rootDir>/src/@layouts/$1',
    '^@layouts$': '<rootDir>/src/@layouts',
    '^@images/(.*)$': '<rootDir>/src/assets/images/$1',
    '^@styles/(.*)$': '<rootDir>/src/assets/styles/$1',
    '^@configured-variables$': '<rootDir>/tests/__mocks__/styleMock.js',
  },
  globals: {
    'vue-jest': {
      tsConfig: './tsconfig.json',
    },
  },
  setupFilesAfterEnv: ['<rootDir>/tests/setup.ts'],
  collectCoverageFrom: [
    'src/**/*.{ts,vue}',
    '!src/main.ts',
  ],
};
