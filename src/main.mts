import { exec } from 'child_process';
import * as path from 'path';
import { fileURLToPath } from 'url';

type Solution = {
  solutionFile: string;
};

type Project = {
  projectFile: string;
};

type Options = {
  configFile: string;
  verify?: boolean;
};

function tsTypeGen(options: Solution & Options): Promise<void>;
function tsTypeGen(options: Project & Options): Promise<void>;
function tsTypeGen(options: (Solution | Project) & Options) {
  const __filename = fileURLToPath(import.meta.url);
  const __dirname = path.dirname(__filename);
  const args: string[] = [
    JSON.stringify(path.join(__dirname, '..', 'bin', 'TSTypeGen.dll')),
  ];

  if ('configFile' in options) {
    args.push(`--cfg=${JSON.stringify(options.configFile)}`);
  } else {
    throw new Error('You must specify a TSTypeGen config file.');
  }

  if (options.verify) {
    args.push('--verify');
  }

  return new Promise<void>((resolve, reject) => {
    exec(`dotnet ${args.join(' ')}`, (error, stdout, stderr) => {
      if (error) {
        if (stderr) {
          process.stdout.write(stderr);
          reject();
        } else {
          reject(error);
        }
      } else {
        if (stdout) {
          process.stdout.write(stdout);
        } else {
          console.info(
            options.verify
              ? 'All type definitions verified.'
              : 'No type definitions changed.'
          );
        }

        resolve();
      }
    });
  });
}

export default tsTypeGen;
