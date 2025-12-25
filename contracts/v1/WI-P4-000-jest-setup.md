# WI-P4-000: Configure Jest for Angular Testing

## Objective
Set up Jest testing framework with @testing-library/angular for the Angular 21 dashboard.

## Context
- The dashboard currently has no test framework configured
- `skipTests: true` is set in `angular.json` schematics
- Need Jest for modern, fast testing with good DX
- This is a prerequisite for WI-P4-003 and WI-P4-004

## Files to Create/Modify

### 1. Install Dependencies
```bash
cd dashboard
npm install -D jest @types/jest jest-preset-angular @testing-library/angular @testing-library/jest-dom
```

### 2. Create `dashboard/jest.config.js`
```javascript
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['<rootDir>/node_modules/', '<rootDir>/dist/'],
  moduleNameMapper: {
    '^src/(.*)$': '<rootDir>/src/$1'
  },
  testMatch: ['**/*.spec.ts'],
  collectCoverageFrom: [
    'src/app/**/*.ts',
    '!src/app/**/*.module.ts',
    '!src/main.ts'
  ]
};
```

### 3. Create `dashboard/setup-jest.ts`
```typescript
import 'jest-preset-angular/setup-jest';
import '@testing-library/jest-dom';
```

### 4. Create `dashboard/tsconfig.spec.json`
```json
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./out-tsc/spec",
    "types": ["jest"],
    "esModuleInterop": true,
    "emitDecoratorMetadata": true
  },
  "include": [
    "src/**/*.spec.ts",
    "src/**/*.d.ts",
    "setup-jest.ts"
  ]
}
```

### 5. Update `dashboard/package.json`
Add/update scripts section:
```json
{
  "scripts": {
    "test": "jest",
    "test:watch": "jest --watch",
    "test:coverage": "jest --coverage"
  }
}
```

## Implementation Steps

1. Run npm install command for all dependencies
2. Create `jest.config.js` in dashboard root
3. Create `setup-jest.ts` in dashboard root
4. Create `tsconfig.spec.json` in dashboard root
5. Update `package.json` scripts
6. Create a simple smoke test to verify setup works

### Smoke Test: `dashboard/src/app/app.spec.ts`
```typescript
describe('Jest Setup', () => {
  it('should run tests', () => {
    expect(true).toBe(true);
  });

  it('should have jest-dom matchers', () => {
    const div = document.createElement('div');
    div.innerHTML = 'Hello';
    document.body.appendChild(div);
    expect(div).toBeInTheDocument();
  });
});
```

## Acceptance Criteria

- [ ] `npm test` runs without errors
- [ ] Jest finds and executes spec files
- [ ] @testing-library/jest-dom matchers work (toBeInTheDocument, etc.)
- [ ] TypeScript compilation works for spec files
- [ ] Watch mode works with `npm run test:watch`

## Verification

```bash
cd dashboard
npm test
# Should output: "Test Suites: 1 passed"
```

## Notes

- Jest is faster than Karma and has better DX
- @testing-library/angular promotes testing user behavior over implementation details
- This setup supports Angular 21 standalone components
