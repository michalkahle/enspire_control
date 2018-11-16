import pyodbc
import subprocess
import pandas as pd

class open_db_connection:
    def __enter__(self):
        cs = 'DRIVER={SQL Server};SERVER=ENSPIRE-PC;DATABASE=EnVisionLite'
        self.connection = pyodbc.connect(cs)
        cursor = self.connection.cursor()
        return cursor
    def __exit__(self, exc_type, exc_value, traceback):
        self.connection.commit()
        self.connection.close()

def execute(sql, *args):
    with db_cursor() as cursor:
        cursor.execute(sql, *args)
        rows = cursor.fetchall()
    return rows, cursor.description

def get_protocol():
    with open_db_connection() as cursor:
        cursor.execute("SELECT PlateMap FROM assayProtocol WHERE protName = 'enspire_control'")
        platemap = cursor.fetchone()[0]
    return platemap

def well_map(q):
    def char_range(c1, c2):
        for c in range(ord(c1), ord(c2)+1):
            yield chr(c)
    plate = {}
    for n in list('abcdefghijklmnop'):
        plate[n] = [0 for g in range(24)]
    for j in q.replace(' ', '').lower().split(','):
        (b, e) = j.split('-') if ('-' in j) else (j, j)
        for nn in char_range(b[:1], e[:1]):
            for ii in range(int(b[1:]) - 1, int(e[1:])):
                plate[nn][ii] = 1
    out = []
    def add_range(row, start, end):
        rn = (ord(row) - 97) * 24
        out.append('%i-%i [6]' % (rn + start, rn + end))
    for row in list('abcdefghijklmnop'):
        start = None
        for ii, val in enumerate(plate[row]):
#             print(ii, val, start)
            if val and not start:
                start = ii + 1
#                 print('set: ', ii)
            elif not val and start:
                add_range(row, start, ii)
                start = None
        if start:
            add_range(row, start, ii+1)
    return ','.join(out)

def update_protocol(wells, ex, em, step):
    def check(p):
        if type(p) != int:
            raise ValueError('Please use integers.')
        if p < 230 or p > 1000:
            raise ValueError('Monochromator range is between 230nm and 1000nm.')
        else:
            return p
    if ex is None or em is None or step is None:
        raise ValueError('All parameters must be specified.')
    if type(step) != int or step < 1 or step > 750:
        raise ValueError('step must be an integer between 1 and 750')

    pp = [None for x in range(8)]
    if type(ex)==int and type(em)==tuple:
        if check(em[0]) - check(ex) < 20:
            raise ValueError('Gap between excitation and emission must be 20nm or more.')
        if check(em[1]) - check(em[0]) < step:
            raise ValueError('Range must not be smaller than step.')
        pp[3] = ex
        pp[4] = em[0]
        pp[5] = em[1]
        pp[6] = step
    elif type(ex)==tuple and type(em)==int:
        if check(em) - check(ex[1]) < 20:
            raise ValueError('Gap between excitation and emission must be 20nm or more.')
        if check(ex[1]) - check(ex[0]) < step:
            raise ValueError('Range must not be smaller than step.')
        pp[7] = em
        pp[0] = ex[0]
        pp[1] = ex[1]
        pp[2] = step
    else:
        raise ValueError('One of ex and em should be set to a range (tuple).')
    with open_db_connection() as cursor:
        cursor.execute("SELECT PlateMap FROM assayProtocol WHERE protName = 'enspire_control'")
        res = cursor.fetchone()[0]
        if wells is not None:
            new_map = well_map(wells)
            start = res.find('GROUP Wells="') + 13
            end = res.find('"', start)
            res = res[:start] + new_map + res[end:]
        script = res.find('Name="SCANMEAS_MC"')
        start = res.find('Params="', script) + 8
        end = res.find('"', start)
        params = res[start:end].split(',')
        pp_orig = [int(x)//1000 for x in params[1:9]]
        for ii, p in enumerate(pp):
            if p is not None:
                params[ii+1] = str(p * 1000)
            else:
                params[ii+1] = str(- abs(pp_orig[ii] * 1000) )
        res2 = res[:start] + ','.join(params) + res[end:]
        sql = """DECLARE @ptrval binary(16)
        SELECT @ptrval = TEXTPTR(PlateMap) FROM AssayProtocol WHERE ProtName = 'enspire_control'
        WRITETEXT AssayProtocol.PlateMap @ptrval ?;"""
        cursor.execute(sql, res2)

def load_enspire(fn, labels=None):
    header, nrows, modality = None, None, None
    modalities = {'Fluorescence':'fi', 'Absorbance':'A'}
    with open(fn, 'r', errors='replace') as ff:
        for (ii, line) in enumerate(ff):
            if 'Well,' in line:
                header = ii
            if 'Basic assay information' in line:
                nrows = ii - header - 2
            if '    Tech,' in line:
                modality = line.split(',')[4][:-1]
    assert nrows and header, 'This file has strange structure!'
    df = pd.read_csv(fn, header=header, nrows=nrows, skip_blank_lines=False)
    rename_dict = {
        'Well' : 'well',
        'MeasA:WavelengthExc' : 'ex',
        'MeasA:WavelengthEms' : 'em',
        'MeasA:Result' : modalities[modality]
    }
    df = df.rename(columns=rename_dict).drop(set(df.columns) - rename_dict.keys(), axis=1)
    df['col'] = df.well.str.slice(1).astype('int')
    df['row'] = df.well.str.slice(0, 1)
    df['file'] = fn[-7:-4]
    if labels is not None:
        df = df.merge(labels, 'left')
    return df

def measure(wells, ex, em, step):
    update_protocol(wells, ex, em, step)
    completed_process = subprocess.run(['EnspireWebClient.exe', 'run', 'enspire_control'], stdout=subprocess.PIPE)
    stdout = completed_process.stdout.decode()
    start = stdout.find('OnExportDone: ') + 14
    end = stdout.find('\r', start)
    csv_path = stdout[start : end]
    return load_enspire(csv_path)