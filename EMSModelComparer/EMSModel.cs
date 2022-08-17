using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitel.Mal;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal.Context.CIM16;
using Monitel.Mal.Context.CIM16.Ext.EMS;
using Monitel.UI.Infrastructure.Services;

namespace EMSModelComparer
{
    internal class EMSModel : IModel
    {
        private FolderWithAppFilesHandler programFolder;
        private FileHandler fileHandler;

        private int organisationRoleIndex;

        private ModelImage reverseModelImage;
        private ModelImage forwardModelImage;

        // Список с изменениями в модели
        private List<string> listOfDifferences;
        private HashSet<string> exceptionPropertyHash; // Перечень свойст, которые исключаются из проверки

        HashSet<Guid> reverseObjectsUids; // Перечень объектов контроля из старой модели
        HashSet<Guid> forwardObjectsUids; // Перечень объектов контроля из новой модели

        // Потоки для работы с файлами		
        private System.IO.StreamWriter swReverse;
        private System.IO.StreamWriter swForward;
        private System.IO.StreamWriter swDiff;
        private System.IO.StreamReader srExcep;

        private IServiceManager Services;

        private Guid OrgUid;


        public string OdbServerName
        {
            get { return fileHandler.OdbServerName; }
            set { fileHandler.OdbServerName = value; }
        }

        public string ReverseOdbInstanseName
        {
            get { return fileHandler.ReverseOdbInstanseName; }
            set { fileHandler.ReverseOdbInstanseName = value; }
        }

        public string ReverseOdbModelVersionId
        {
            get { return fileHandler.ReverseOdbModelVersionId; }
            set { fileHandler.ReverseOdbModelVersionId = value; }
        }

        public string ForwardOdbInstanseName
        {
            get { return fileHandler.ForwardOdbInstanseName; }
            set { fileHandler.ForwardOdbInstanseName = value; }
        }

        public string ForwardOdbModelVersionId
        {
            get { return fileHandler.ForwardOdbModelVersionId; }
            set { fileHandler.ForwardOdbModelVersionId = value; }
        }

        public int OrganisationRoleIndex
        {
            get { return organisationRoleIndex; }
            set { organisationRoleIndex = value; }
        }

        public Guid OrganisationUID
        {
            get { return OrgUid; }
            set { OrgUid = value; }
        }

        internal EMSModel()
        {
            programFolder = new FolderWithAppFilesHandler();
            programFolder.CreateFolderWithAppFilesIfAbsent();

            fileHandler = new FileHandler(programFolder);
            fileHandler.CreateAppFiles();
            fileHandler.ReadDataFromConfigFile();
        }

        public void CompareModels()
        {
            try
            {
                UpdateConfigFile();

                ConnectToReverseModelimage();
                ConnectToForwardModelImage();

                SetStreamWriters();
                SetStreamReaders();
                //SetHashSets();

                ChooseOrganisationRoleToCompare();

                WriteLogFiles();

                // Отладочная информация
                MessageBox.Show("В списке исключений: " + exceptionPropertyHash.Count());
                MessageBox.Show("Добавлено reverse объектов: " + reverseObjectsUids.Count());
                MessageBox.Show("Добавлено forward объектов: " + forwardObjectsUids.Count());

                // Далее составляется список изменений в моделях
                listOfDifferences.Add("UID;Действие;Текст;Имя объекта;Класс объекта;Ответственный за моделирование");   // Шапка таблицы

                HashSet<Guid> deletedObjectsUids = new HashSet<Guid>(reverseObjectsUids);   // Перечень объектов удаленных из модели
                deletedObjectsUids.ExceptWith(forwardObjectsUids);

                MessageBox.Show("Удаленных объектов: " + deletedObjectsUids.Count());

                foreach (Guid uid in deletedObjectsUids)
                {
                    ODUSVWriteAddedOrDeletedObject(uid, reverseModelImage, "deleted");
                }

                HashSet<Guid> addedObjectsUids = new HashSet<Guid>(forwardObjectsUids); // Перечень объектов добавленных в модель
                addedObjectsUids.ExceptWith(reverseObjectsUids);

                MessageBox.Show("Добавленных объектов: " + addedObjectsUids.Count());

                foreach (Guid uid in addedObjectsUids)
                {
                    ODUSVWriteAddedOrDeletedObject(uid, forwardModelImage, "added");
                }

                // Добавление в список измененных объектов
                HashSet<Guid> otherMTNUids = new HashSet<Guid>(reverseObjectsUids); // Перечень объектов добавленных в модель
                otherMTNUids.IntersectWith(forwardObjectsUids);

                MessageBox.Show("Оставшихся объектов: " + otherMTNUids.Count());

                // Составление списка изменений
                foreach (Guid uid in otherMTNUids)
                {
                    ODUSVFindChanges(uid);
                }


                // Заполнение файла с измененяим
                foreach (string text in listOfDifferences)
                {
                    swDiff.WriteLine(text);
                }

                swDiff.Close();
                MessageBox.Show("Скрипт выполнен успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                swReverse.Close();
                swForward.Close();
                swDiff.Close();
                srExcep.Close();
            }
        }

        private void UpdateConfigFile()
        {
            // Запись данных в config файл
            System.IO.StreamWriter swConfig = new System.IO.StreamWriter(fileHandler.ConfigFilePath, false, System.Text.Encoding.Default);
            swConfig.WriteLine("OdbServerName;" + fileHandler.OdbServerName);
            swConfig.WriteLine("ReverseOdbInstanseName;" + fileHandler.ReverseOdbInstanseName);
            swConfig.WriteLine("ReverseOdbModelVersionId;" + fileHandler.ReverseOdbModelVersionId);
            swConfig.WriteLine("ForwardOdbInstanseName;" + fileHandler.ForwardOdbInstanseName);
            swConfig.WriteLine("ForwardOdbModelVersionId;" + fileHandler.ForwardOdbModelVersionId);
            swConfig.Close();
        }

        private void ConnectToReverseModelimage()
        {
            // Подключение к исходной модели
            Monitel.Mal.Providers.MalContextParams reverseContext = new Monitel.Mal.Providers.MalContextParams()
            {
                OdbServerName = fileHandler.OdbServerName,
                OdbInstanseName = fileHandler.ReverseOdbInstanseName,
                OdbModelVersionId = Convert.ToInt32(fileHandler.ReverseOdbModelVersionId),
            };
            Monitel.Mal.Providers.Mal.MalProvider ReverseDataProvider = new Monitel.Mal.Providers.Mal.MalProvider(reverseContext, Monitel.Mal.Providers.MalContextMode.Open, "test", -1);
            reverseModelImage = new ModelImage(ReverseDataProvider, true);
        }

        private void ConnectToForwardModelImage()
        {
            // Подключение к сравниваемой модели
            Monitel.Mal.Providers.MalContextParams forwardContext = new Monitel.Mal.Providers.MalContextParams()
            {
                OdbServerName = fileHandler.OdbServerName,
                OdbInstanseName = fileHandler.ForwardOdbInstanseName,
                OdbModelVersionId = Convert.ToInt32(fileHandler.ForwardOdbModelVersionId),
            };
            Monitel.Mal.Providers.Mal.MalProvider ForwardDataProvider = new Monitel.Mal.Providers.Mal.MalProvider(forwardContext, Monitel.Mal.Providers.MalContextMode.Open, "test", -1);
            forwardModelImage = new ModelImage(ForwardDataProvider, true);
        }

        private void SetStreamWriters()
        {
            // Путь к реверс файлу
            string reversePath = programFolder.PathToScriptFiles + @"\ReverseHash.csv";
            System.IO.StreamWriter swReverse = new System.IO.StreamWriter(reversePath, false, System.Text.Encoding.Default);

            // Путь к форвард файлу
            string forwardPath = programFolder.PathToScriptFiles + @"\ForwardHash.csv";
            System.IO.StreamWriter swForward = new System.IO.StreamWriter(forwardPath, false, System.Text.Encoding.Default);

            // Путь к главному файлу с изменениями
            string differencePath = programFolder.PathToScriptFiles + @"\Перечень изменений.csv";
            System.IO.StreamWriter swDiff = new System.IO.StreamWriter(differencePath, false, System.Text.Encoding.Default);
        }

        private void SetStreamReaders()
        {
            // Путь к файлу со свойствами, исключаемыми из проверки
            string exceptionPropertyListPath = programFolder.PathToScriptFiles + @"\ExceptionPropertyList.csv";
            System.IO.StreamReader srExcep = new System.IO.StreamReader(exceptionPropertyListPath, System.Text.Encoding.Default, false);
        }

        /*private void SetHashSets()
        {
            HashSet<Guid> reverseObjectsUids = new HashSet<Guid>(); // Перечень объектов контроля из старой модели
            HashSet<Guid> forwardObjectsUids = new HashSet<Guid>(); // Перечень объектов контроля из новой модели

            exceptionPropertyHash = new HashSet<string>(); // Перечень свойст, которые исключаются из проверки
        }*/

        private void ChooseOrganisationRoleToCompare()
        {
            // Поиск роли организации, по которой выполняется сравнение моделей
            Guid roleTypeUid;
            if (OrganisationRoleIndex == 0)
            {
                roleTypeUid = new Guid("1000161E-0000-0000-C000-0000006D746C"); // Контроль в МТН
                ODUSVCreatingListOfComparedObjectsMTN(reverseObjectsUids, ODUSVFindOrganisationRole(roleTypeUid, OrgUid), reverseModelImage);
                ODUSVCreatingListOfComparedObjectsMTN(forwardObjectsUids, ODUSVFindOrganisationRole(roleTypeUid, OrgUid), forwardModelImage);
            }
            else if (OrganisationRoleIndex == 1)
            {
                roleTypeUid = new Guid("10001672-0000-0000-C000-0000006D746C"); // Контроль в КПОС
                MessageBox.Show("Проверка изменений по объектам, контролируемым в КПОС, ещё не разработана");
            }
            else if (OrganisationRoleIndex == 2)
            {
                roleTypeUid = new Guid("10001669-0000-0000-C000-0000006D746C"); // Контроль в МУН
                MessageBox.Show("Проверка изменений по объектам, контролируемым в МУН, ещё не разработана");
            }
        }

        Guid ODUSVFindOrganisationRole(Guid roleTypeUid, Guid organisationUid)
        {
            Guid result = new Guid();

            var organisation = forwardModelImage.GetObject<Organisation>(organisationUid);
            foreach (var role in organisation.Roles)
            {
                if (role.Category.Uid == roleTypeUid)
                {
                    result = role.Uid;
                    break;
                }
            }

            return result;
        }

        // Метод создания списка с UID'ами объектов, участвующих в МТН
        void ODUSVCreatingListOfComparedObjectsMTN(HashSet<Guid> hashOfObjects, Guid Uid, ModelImage mImg)
        {
            var mObjects = mImg.GetObjects<IdentifiedObject>();
            var orgRole = mImg.GetObject<OrganisationRole>(Uid);

            foreach (IdentifiedObject obj in orgRole.Objects)
            {
                // Добавляем контролируемое оборудование
                hashOfObjects.Add(obj.Uid);

                // Добавляем все дочерние объекты
                ODUSVCreatingListOfChildObjects(obj, hashOfObjects);
            }
        }

        // Рекурсивный метод добавления в список дочерних объектов
        void ODUSVCreatingListOfChildObjects(IdentifiedObject currentObj, HashSet<Guid> hashOfChildObjects)
        {
            if (currentObj.ChildObjects.Count() > 0)
            {
                foreach (IdentifiedObject childObj in currentObj.ChildObjects)
                {
                    // Добавляем в список оборудование
                    hashOfChildObjects.Add(childObj.Uid);

                    // Проверяем и добавляем в список все связанные метеостанции и их дочерние объекты
                    ODUSVFindWeatherStations(childObj, hashOfChildObjects);

                    // Проверяем и добавляем в список все связанные уставки ступеней РЗА
                    ODUSVFindPEStageSetPoints(childObj, hashOfChildObjects);

                    // Рекурсивно добавляем в список все дочерние объекты
                    ODUSVCreatingListOfChildObjects(childObj, hashOfChildObjects);
                }
            }
        }

        // Метод добавления в список связанных с оборудованием метеостанций
        void ODUSVFindWeatherStations(IdentifiedObject obj, HashSet<Guid> hashOfWeatherStations)
        {
            if (obj is Equipment)
            {
                foreach (WeatherStation ws in (obj as Equipment).WeatherStation)
                {
                    hashOfWeatherStations.Add(ws.Uid);

                    ODUSVCreatingListOfChildObjects(obj, hashOfWeatherStations);
                }
            }
            else if (obj is Terminal)
            {
                foreach (WeatherStation ws in (obj as Terminal).WeatherStations)
                {
                    hashOfWeatherStations.Add(ws.Uid);

                    ODUSVCreatingListOfChildObjects(obj, hashOfWeatherStations);
                }
            }
        }

        // Метод добавления в список уставок ступеней РЗА
        void ODUSVFindPEStageSetPoints(IdentifiedObject obj, HashSet<Guid> hashOfPEStageSetPoints)
        {
            if (obj is PEStage)
            {
                foreach (PEStageSetpoint pessp in (obj as PEStage).PESetpoints)
                {
                    hashOfPEStageSetPoints.Add(pessp.Uid);
                }
            }
        }

        private void WriteLogFiles()
        {
            // Запись списка объектов из проверяемых моделей в соответствующие файлы
            foreach (Guid uid in reverseObjectsUids)
            {
                swReverse.WriteLine(uid);
            }
            foreach (Guid uid in forwardObjectsUids)
            {
                swForward.WriteLine(uid);
            }

            // Составление списка со связями, исключенными из проверки
            string line;
            while ((line = srExcep.ReadLine()) != null)
            {
                exceptionPropertyHash.Add(line);
            }
        }

        // Метод записи добавленного/удаленного объекта ИМ по UID в общий список изменений
        void ODUSVWriteAddedOrDeletedObject(Guid uid, ModelImage mImg, string state)
        {
            var selectedObject = mImg.GetObject<IdentifiedObject>(uid);
            switch (state)
            {
                case "deleted":
                    listOfDifferences.Add(Convert.ToString(selectedObject.Uid) + ";Удаление;Удален объект класса " + selectedObject.ClassName() + ";" + selectedObject.name + ";" + selectedObject.ClassName());
                    break;
                case "added":
                    listOfDifferences.Add(Convert.ToString(selectedObject.Uid) + ";Добавление;Добавлен объект класса " + selectedObject.ClassName() + ";" + selectedObject.name + ";" + selectedObject.ClassName());
                    break;
            }
        }

        void ODUSVFindChanges(Guid uid)
        {
            var reverseObject = reverseModelImage.GetObject</*IdentifiedObject*/IMalObject>(uid);
            // Проверка на принадлежность к именуемым объектам или уставкам РЗА
            if (reverseObject != null)
            {
                var forwardObject = forwardModelImage.GetObject</*IdentifiedObject*/IMalObject>(uid);

                // Если проверяемый объект - IMalObject
                if (reverseObject is IMalObject)
                {
                    Type t = reverseObject.GetType();


                    //(int)Convert.ChangeType(flt, typeof(int));
                    var iMORev = reverseObject as IMalObject;
                    var iMOForw = forwardObject as IMalObject;

                    ODUSVPropertyReflectInfo(iMORev, iMOForw, exceptionPropertyHash, listOfDifferences);
                }

                #region             
                // Если проверяемый объект - участок линии переменного тока
                if (reverseObject is ACLineSegment)
                {
                    ACLineSegment rObj = reverseObject as ACLineSegment;
                    ACLineSegment fObj = forwardObject as ACLineSegment;
                    ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
                }

                // Если проверяемый объект - аналог
                else if (reverseObject is Analog)
                {
                    Analog rObj = reverseObject as Analog;
                    Analog fObj = forwardObject as Analog;
                    ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
                }

                // Если проверяемый объект - дискрет
                else if (reverseObject is Discrete)
                {
                    Discrete rObj = reverseObject as Discrete;
                    Discrete fObj = forwardObject as Discrete;
                    ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
                }

                // Если проверяемый объект - ошиновка
                else if (reverseObject is BusArrangement)
                {
                    BusArrangement rObj = reverseObject as BusArrangement;
                    BusArrangement fObj = forwardObject as BusArrangement;
                    ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
                }

                // Если проверяемый объект - набор эксплуатационных ограничений		
                else if (reverseObject is OperationalLimitSet)
                {
                    OperationalLimitSet rObj = reverseObject as OperationalLimitSet;
                    OperationalLimitSet fObj = forwardObject as OperationalLimitSet;
                    ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
                }

                // Если проверяемый объект - предел тока		
                else if (reverseObject is CurrentLimit)
                {
                    CurrentLimit cLRev = reverseObject as CurrentLimit;
                    CurrentLimit cLForw = forwardObject as CurrentLimit;

                    ODUSVPropertyReflectInfo(cLRev, cLForw, exceptionPropertyHash, listOfDifferences);
                }

                //CurrentFlowUnbalanceLimit
                // Если проверяемый объект - предел несимметрии токов фаз
                else if (reverseObject is CurrentFlowUnbalanceLimit)
                {
                    CurrentFlowUnbalanceLimit cFULRev = reverseObject as CurrentFlowUnbalanceLimit;
                    CurrentFlowUnbalanceLimit cFULForw = forwardObject as CurrentFlowUnbalanceLimit;

                    ODUSVPropertyReflectInfo(cFULRev, cFULForw, exceptionPropertyHash, listOfDifferences);
                }

                // Если проверяемый объект - кривая зависимости тока от температуры
                else if (reverseObject is CurrentVsTemperatureLimitCurve)
                {
                    CurrentVsTemperatureLimitCurve cVTLCRev = reverseObject as CurrentVsTemperatureLimitCurve;
                    CurrentVsTemperatureLimitCurve cVTLCForw = forwardObject as CurrentVsTemperatureLimitCurve;

                    ODUSVPropertyReflectInfo(cVTLCRev, cVTLCForw, exceptionPropertyHash, listOfDifferences);
                }

                //PotentialTransformer
                // Если проверяемый объект - трансформатор напряжения
                else if (reverseObject is PotentialTransformer)
                {
                    PotentialTransformer pTRev = reverseObject as PotentialTransformer;
                    PotentialTransformer pTForw = forwardObject as PotentialTransformer;

                    ODUSVPropertyReflectInfo(pTRev, pTForw, exceptionPropertyHash, listOfDifferences);
                }

                // PotentialTransformerWinding
                // Если проверяемый объект - обмотка трансформатора напряжения
                else if (reverseObject is PotentialTransformerWinding)
                {
                    PotentialTransformerWinding pTWRev = reverseObject as PotentialTransformerWinding;
                    PotentialTransformerWinding pTWForw = forwardObject as PotentialTransformerWinding;

                    ODUSVPropertyReflectInfo(pTWRev, pTWForw, exceptionPropertyHash, listOfDifferences);
                }

                // Terminal
                // Если проверяемый объект - полюс оборудования
                else if (reverseObject is Terminal)
                {
                    Terminal tRev = reverseObject as Terminal;
                    Terminal tForw = forwardObject as Terminal;

                    ODUSVPropertyReflectInfo(tRev, tForw, exceptionPropertyHash, listOfDifferences);
                }

                // WaveTrap
                // Если проверяемый объект - ВЧЗ заградитель
                else if (reverseObject is WaveTrap)
                {
                    WaveTrap wTRev = reverseObject as WaveTrap;
                    WaveTrap wTForw = forwardObject as WaveTrap;

                    ODUSVPropertyReflectInfo(wTRev, wTForw, exceptionPropertyHash, listOfDifferences);
                }

                // Breaker
                // Если проверяемый объект - выключатель
                else if (reverseObject is Breaker)
                {
                    Breaker bRev = reverseObject as Breaker;
                    Breaker bForw = forwardObject as Breaker;

                    ODUSVPropertyReflectInfo(bRev, bForw, exceptionPropertyHash, listOfDifferences);
                }

                // AnalogValue
                // Если проверяемый объект - аналоговое значение
                else if (reverseObject is AnalogValue)
                {
                    AnalogValue aVRev = reverseObject as AnalogValue;
                    AnalogValue aVForw = forwardObject as AnalogValue;

                    ODUSVPropertyReflectInfo(aVRev, aVForw, exceptionPropertyHash, listOfDifferences);
                }

                // DiscreteValue
                // Если проверяемый объект - дискретное значение
                else if (reverseObject is DiscreteValue)
                {
                    DiscreteValue dVRev = reverseObject as DiscreteValue;
                    DiscreteValue dVForw = forwardObject as DiscreteValue;

                    ODUSVPropertyReflectInfo(dVRev, dVForw, exceptionPropertyHash, listOfDifferences);
                }

                // CurrentTransformer
                // Если проверяемый объект - трансформатор тока
                else if (reverseObject is CurrentTransformer)
                {
                    CurrentTransformer cTRev = reverseObject as CurrentTransformer;
                    CurrentTransformer cTForw = forwardObject as CurrentTransformer;

                    ODUSVPropertyReflectInfo(cTRev, cTForw, exceptionPropertyHash, listOfDifferences);
                }

                // CurrentTransformerWinding
                // Если проверяемый объект - обмотка трансформатора тока
                else if (reverseObject is CurrentTransformerWinding)
                {
                    CurrentTransformerWinding cTWRev = reverseObject as CurrentTransformerWinding;
                    CurrentTransformerWinding cTWForw = forwardObject as CurrentTransformerWinding;

                    ODUSVPropertyReflectInfo(cTWRev, cTWForw, exceptionPropertyHash, listOfDifferences);
                }

                // Disconnector
                // Если проверяемый объект - разъединитель
                else if (reverseObject is Disconnector)
                {
                    Disconnector dRev = reverseObject as Disconnector;
                    Disconnector dForw = forwardObject as Disconnector;

                    ODUSVPropertyReflectInfo(dRev, dForw, exceptionPropertyHash, listOfDifferences);
                }

                // Line
                // Если проверяемый объект - ЛЭП
                else if (reverseObject is Line)
                {
                    Line lRev = reverseObject as Line;
                    Line lForw = forwardObject as Line;

                    ODUSVPropertyReflectInfo(lRev, lForw, exceptionPropertyHash, listOfDifferences);
                }

                // PowerTransformer
                // Если проверяемый объект - силовой трансформатор
                else if (reverseObject is PowerTransformer)
                {
                    PowerTransformer pTRev = reverseObject as PowerTransformer;
                    PowerTransformer pTForw = forwardObject as PowerTransformer;

                    ODUSVPropertyReflectInfo(pTRev, pTForw, exceptionPropertyHash, listOfDifferences);
                }

                // PowerTransformerEnd
                // Если проверяемый объект - обмотка трансформатора
                else if (reverseObject is PowerTransformerEnd)
                {
                    PowerTransformerEnd pTERev = reverseObject as PowerTransformerEnd;
                    PowerTransformerEnd pTEForw = forwardObject as PowerTransformerEnd;

                    ODUSVPropertyReflectInfo(pTERev, pTEForw, exceptionPropertyHash, listOfDifferences);
                }

                // CurrentVsTapStepLimitCurve
                // Если проверяемый объект - кривая зависимости тока от регулировачного ответвления
                else if (reverseObject is CurrentVsTapStepLimitCurve)
                {
                    CurrentVsTapStepLimitCurve cVTSLCRev = reverseObject as CurrentVsTapStepLimitCurve;
                    CurrentVsTapStepLimitCurve cVTSLCForw = forwardObject as CurrentVsTapStepLimitCurve;

                    ODUSVPropertyReflectInfo(cVTSLCRev, cVTSLCForw, exceptionPropertyHash, listOfDifferences);
                }

                // RatioTapChanger
                // Если проверяемый объект - ПБВ/РПН
                else if (reverseObject is RatioTapChanger)
                {
                    RatioTapChanger rTCRev = reverseObject as RatioTapChanger;
                    RatioTapChanger rTCForw = forwardObject as RatioTapChanger;

                    ODUSVPropertyReflectInfo(rTCRev, rTCForw, exceptionPropertyHash, listOfDifferences);
                }

                // TransformerMeshImpedance
                // Если проверяемый объект - трансформаторная ветвь многоугольника
                else if (reverseObject is TransformerMeshImpedance)
                {
                    TransformerMeshImpedance tMIRev = reverseObject as TransformerMeshImpedance;
                    TransformerMeshImpedance tMIForw = forwardObject as TransformerMeshImpedance;

                    ODUSVPropertyReflectInfo(tMIRev, tMIForw, exceptionPropertyHash, listOfDifferences);
                }

                // LoadSheddingEquipment
                // Если проверяемый объект - автоматика ограничения потребления
                else if (reverseObject is LoadSheddingEquipment)
                {
                    LoadSheddingEquipment lSERev = reverseObject as LoadSheddingEquipment;
                    LoadSheddingEquipment lSEForw = forwardObject as LoadSheddingEquipment;

                    ODUSVPropertyReflectInfo(lSERev, lSEForw, exceptionPropertyHash, listOfDifferences);
                }

                // GenericPSR
                // Если проверяемый объект - прочий энергообъект
                else if (reverseObject is GenericPSR)
                {
                    GenericPSR gPSRRev = reverseObject as GenericPSR;
                    GenericPSR gPSRForw = forwardObject as GenericPSR;

                    ODUSVPropertyReflectInfo(gPSRRev, gPSRForw, exceptionPropertyHash, listOfDifferences);
                }

                // GenerationUnloadingStage
                // Если проверяемый объект - ступень ОГ
                else if (reverseObject is GenerationUnloadingStage)
                {
                    GenerationUnloadingStage gUSRev = reverseObject as GenerationUnloadingStage;
                    GenerationUnloadingStage gUSForw = forwardObject as GenerationUnloadingStage;

                    ODUSVPropertyReflectInfo(gUSRev, gUSForw, exceptionPropertyHash, listOfDifferences);
                }

                // LoadSheddingStage
                // Если проверяемый объект - ступень ОН
                else if (reverseObject is LoadSheddingStage)
                {
                    LoadSheddingStage lSSRev = reverseObject as LoadSheddingStage;
                    LoadSheddingStage lSSForw = forwardObject as LoadSheddingStage;

                    ODUSVPropertyReflectInfo(lSSRev, lSSForw, exceptionPropertyHash, listOfDifferences);
                }

                // LimitExpression
                // Если проверяемый объект - выражение эксплуатационного ограничения
                else if (reverseObject is LimitExpression)
                {
                    LimitExpression lERev = reverseObject as LimitExpression;
                    LimitExpression lEForw = forwardObject as LimitExpression;

                    ODUSVPropertyReflectInfo(lERev, lEForw, exceptionPropertyHash, listOfDifferences);
                }

                // PSRMeasOperand
                // Если проверяемый объект - операнд измерения энергообъекта
                else if (reverseObject is PSRMeasOperand)
                {
                    PSRMeasOperand pSRMORev = reverseObject as PSRMeasOperand;
                    PSRMeasOperand pSRMOForw = forwardObject as PSRMeasOperand;

                    ODUSVPropertyReflectInfo(pSRMORev, pSRMOForw, exceptionPropertyHash, listOfDifferences);
                }

                // PEStageSetpoint
                // Если проверяемый объект - уставка ступени РЗА
                else if (reverseObject is PEStageSetpoint)
                {
                    PEStageSetpoint pESSRev = reverseObject as PEStageSetpoint;
                    PEStageSetpoint pESSForw = forwardObject as PEStageSetpoint;

                    ODUSVPropertyReflectInfo(pESSRev, pESSForw, exceptionPropertyHash, listOfDifferences);
                }

                // WeatherStation
                // Если проверяемый объект - метеостанция
                else if (reverseObject is WeatherStation)
                {
                    WeatherStation wSRev = reverseObject as WeatherStation;
                    WeatherStation wSForw = forwardObject as WeatherStation;

                    ODUSVPropertyReflectInfo(wSRev, wSForw, exceptionPropertyHash, listOfDifferences);
                }

                #endregion
            }
            else
            {
                try
                {
                    // PEStageSetpoint
                    // Если проверяемый объект - уставка ступени РЗА
                    var reversPESetpoint = reverseModelImage.GetObject<PEStageSetpoint>(uid);

                    if (reversPESetpoint != null)
                    {
                        var forwardPESetpoint = forwardModelImage.GetObject<PEStageSetpoint>(uid);
                        ODUSVPropertyReflectInfo(reversPESetpoint, forwardPESetpoint, exceptionPropertyHash, listOfDifferences);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "Vot zdes ");
                }
            }
        }

        // Рефлексия. Метод перебирает все свойства класса
        void ODUSVPropertyReflectInfo<T>(T objRev, T objForw, HashSet<string> exHash, List<string> listOfDiffs) where T : class
        {
            // В переменную задается тип выбранного объекта
            Type t = typeof(T);

            // В переменную заносятся все реализуемые выбранным классом интерфейсы
            var interfaces = t.GetInterfaces();
            // Циклический проход по всем интерфейсам
            foreach (Type interf in interfaces)
            {
                // Дальнейшую обработку проходят все интерфейсы, кроме IMalObject
                if (interf.Name != "IMalObject")
                {
                    // Циклический проход по всем свойствам выбранного интерфейса
                    foreach (System.Reflection.PropertyInfo prop in interf.GetProperties())
                    {
                        ODUSVCheckingIdentityOfProperty(exHash, listOfDiffs, objRev, objForw, prop);
                    }
                }

            }

            // В переменную заносятся все имеющиеся у выбранного класса свойства (только
            // те, что имеются у этого класса, а не наследуются)
            System.Reflection.PropertyInfo[] propNames = t.GetProperties();

            // Циклический проход по всем свойствам выбранного класса
            foreach (System.Reflection.PropertyInfo prop in propNames)
            {
                ODUSVCheckingIdentityOfProperty(exHash, listOfDiffs, objRev, objForw, prop);
            }
        }

        // Метод проверки идентичности свойства у двух объектов ИМ
        void ODUSVCheckingIdentityOfProperty<T>(HashSet<string> _exHash, List<string> _listOfDiffs, T _objRev, T _objForw, System.Reflection.PropertyInfo _prop) where T : class
        {
            try
            {
                // В переменную задается тип выбранного объекта
                Type t = typeof(T);

                // Проверка отсуствия выбранного свойства в списке исключений
                if (_exHash.Contains(_prop.Name) != true)
                {
                    // В переменную задается значение проверяемого свойства выбранного объекта
                    var vals = _prop.GetValue(_objRev);
                    var valsForw = _prop.GetValue(_objForw);

                    // Если выбранное свойство содержит массив данных, то выполняется обработка массива
                    if (vals is Array)
                    {
                        // Если выбранное свойство - это объект типа System.Byte[], 
                        // то выполняется проверка объекта на принадлежность к классу Curve
                        if (_prop.PropertyType.ToString() == "System.Byte[]" && _objRev is Curve)
                        {
                            // Получаем точки каждой кривой в обеих моделях
                            var curvePointsRev = (_objRev as Curve).GetCurvePoints();
                            var curvePointsForw = (_objForw as Curve).GetCurvePoints();

                            string stringOfPointsRev = String.Empty;
                            string stringOfPointsForw = String.Empty;

                            // По аналогии с проверкой связи один ко многим создаем из координат точек сплошные строки и сравниваем их
                            for (int i = 0; i < curvePointsRev.xvalue.Count(); i++)
                            { stringOfPointsRev = stringOfPointsRev + curvePointsRev.xvalue[i] + curvePointsRev.y1value[i]; }
                            for (int i = 0; i < curvePointsForw.xvalue.Count(); i++)
                            { stringOfPointsForw = stringOfPointsForw + curvePointsForw.xvalue[i] + curvePointsForw.y1value[i]; }

                            if (stringOfPointsRev != stringOfPointsForw)
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                        "' изменилось значение в свойстве '" + _prop.Name + "'. Было " + "points1" + ", стало " +
                                        "points2" + ";" + ODUSVGetObjectName(_objRev) + ";" +
                                        (_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
                        }

                        else if (_prop.PropertyType.ToString() == "Monitel.Mal.Context.CIM16.BranchGroupTerminal[]" && _objRev is Terminal)
                        {
                            // ПЕРЕПИСАТЬ В ОТДЕЛЬНЫЙ МЕТОД
                            // Добавление UID'ов объектов в сортированный список
                            SortedList<string, string> reverseList = new SortedList<string, string>();
                            SortedList<string, string> forwardList = new SortedList<string, string>();

                            foreach (IMalObject val in vals as Array)
                            {
                                reverseList.Add(val.Uid.ToString(), val.Uid.ToString());
                            }
                            foreach (IMalObject val in valsForw as Array)
                            {
                                forwardList.Add(val.Uid.ToString(), val.Uid.ToString());
                            }

                            // Составление строк идентификаторов у каждого из проверяемых объектов
                            string reverseCode = "";
                            string forwardCode = "";
                            foreach (string str in reverseList.Keys)
                            {
                                reverseCode = reverseCode + str;
                            }
                            foreach (string str in forwardList.Keys)
                            {
                                forwardCode = forwardCode + str;
                            }

                            if (reverseCode != forwardCode)
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                    "' изменилась связь " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
                                    (_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
                        }

                        else
                        {
                            // Проверка значений в обеих моделях на null
                            if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                "' изменилась связь " + _prop.Name + ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() +
                                ";" + ODUSVGetModelingAuthoritySet(_objRev));

                            if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                "' изменилась связь " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
                                (_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));

                            if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null)
                            {
                                // Добавление UID'ов объектов в сортированный список
                                SortedList<string, string> reverseList = new SortedList<string, string>();
                                SortedList<string, string> forwardList = new SortedList<string, string>();

                                foreach (IMalObject val in vals as Array)
                                {
                                    reverseList.Add(val.Uid.ToString(), val.Uid.ToString());
                                }
                                foreach (IMalObject val in valsForw as Array)
                                {
                                    forwardList.Add(val.Uid.ToString(), val.Uid.ToString());
                                }

                                // Составление строк идентификаторов у каждого из проверяемых объектов
                                string reverseCode = "";
                                string forwardCode = "";
                                foreach (string str in reverseList.Keys)
                                {
                                    reverseCode = reverseCode + str;
                                }
                                foreach (string str in forwardList.Keys)
                                {
                                    forwardCode = forwardCode + str;
                                }

                                if (reverseCode != forwardCode)
                                    _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                        "' изменилась связь " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
                                        (_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
                            }
                        }
                    }
                    // Если выбранное свойство - это не массив данных
                    else
                    {
                        // Если выбранное свойство строковое, числовое или булево
                        if (_prop.PropertyType.ToString() == "System.String" || _prop.PropertyType.ToString() == "System.Boolean" ||
                                    _prop.PropertyType.ToString() == "System.Nullable`1[System.Double]" || _prop.PropertyType.ToString() == "System.Double"
                                    || _prop.PropertyType.ToString() == "System.Int32" || _prop.PropertyType.ToString() == "System.Int64")
                        {

                            // Проверка значений в обеих моделях на null
                            if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
                            {
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                "' изменилось значение в свойстве '" + _prop.Name + "'. Было 'не задан'" + ", стало " + _prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") +
                                ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
                                ODUSVGetModelingAuthoritySet(_objRev));
                            }
                            else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
                            {
                                if ((_objRev as IdentifiedObject).Uid == new Guid("A8DFF2C0-68F3-4F97-B0AE-58ABAE838B00")) MessageBox.Show(_prop.Name + " = " + _prop.GetValue(_objRev));
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                "' изменилось значение в свойстве '" + _prop.Name + "'. Было " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", стало 'не задан'" +
                                ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
                                ODUSVGetModelingAuthoritySet(_objRev));
                            }
                            else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null)
                            {
                                if (_prop.GetValue(_objRev).ToString() != _prop.GetValue(_objForw).ToString())
                                {
                                    if (_objRev is IdentifiedObject && _objForw is IdentifiedObject)
                                        _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                        "' изменилось значение в свойстве '" + _prop.Name + "'. Было " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", стало " +
                                        _prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") + ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() +
                                        ";" + ODUSVGetModelingAuthoritySet(_objRev));
                                    else
                                        _listOfDiffs.Add(Convert.ToString((_objRev as IMalObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                        "' изменилось значение в свойстве '" + _prop.Name + "'. Было " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", стало " +
                                        _prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") + ";" + (_objRev as IMalObject).ClassName()/*name*/ + ";" + (_objRev as IMalObject).ClassName() +
                                        ";" + ODUSVGetModelingAuthoritySet(_objRev));
                                }
                            }
                        }

                        // Если выбранное свойство - это объект какого-то класса
                        else
                        {
                            // Проверка значений в обеих моделях на null
                            if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
                            {
                                string line = "";
                                // Проверка на полюс
                                if (valsForw is Terminal)
                                {
                                    line = ODUSVGetObjectName(valsForw);

                                }
                                else if (valsForw is IdentifiedObject)
                                {
                                    line = ODUSVGetObjectName(valsForw);
                                }
                                else line = ODUSVGetObjectName(valsForw);

                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                "' изменилось значение в свойстве '" + _prop.Name + "'. Было 'не задан'" + ", стало " + line +
                                ";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
                                ODUSVGetModelingAuthoritySet(_objRev));
                            }
                            else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
                            {
                                string line = "";
                                // Проверка на полюс
                                if (vals is Terminal)
                                    line = ODUSVGetObjectName(vals);
                                else if (vals is IdentifiedObject)
                                    line = ODUSVGetObjectName(vals);
                                else line = ODUSVGetObjectName(vals);
                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                    "' изменилось значение в свойстве '" + _prop.Name + "'. Было " + line + ", стало 'не задан'" +
                                    ";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() + ";"
                                    + ODUSVGetModelingAuthoritySet(_objRev));
                            }
                            else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null &&
                                        _prop.GetValue(_objRev).ToString() != _prop.GetValue(_objForw).ToString())
                            {
                                string line1 = "";
                                string line2 = "";
                                // Проверка на полюс
                                if (vals is Terminal)
                                    line1 = ODUSVGetObjectName(vals);
                                else if (vals is IdentifiedObject)
                                    line1 = ODUSVGetObjectName(vals);
                                else line1 = ODUSVGetObjectName(vals);

                                if (valsForw is Terminal)
                                    line2 = ODUSVGetObjectName(valsForw);
                                else if (valsForw is IdentifiedObject)
                                    line2 = ODUSVGetObjectName(valsForw);
                                else line2 = ODUSVGetObjectName(valsForw);

                                _listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
                                        "' изменилось значение в свойстве '" + _prop.Name + "'. Было " + line1 + ", стало " +
                                        line2 + ";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() +
                                        ";" + ODUSVGetModelingAuthoritySet(_objRev));
                            }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() + "\n" + _prop.Name + "\n" + _prop.PropertyType.ToString() + "\n" + (_objRev as IMalObject).Uid);
            }
        }

        // Метод определения имении объекта ИМ
        string ODUSVGetObjectName<T>(/*IMalObject*/T inputObject) where T : class
        {
            string result = String.Empty;
            if (inputObject is IMalObject)
            {
                // Переопрделяем объект для дальнейшей работы
                IMalObject obj = inputObject as IMalObject;

                // Если объект является именуемым объектом, но не полюсом и не точкой подключения
                if ((obj is IdentifiedObject) == true && (obj is Terminal) == false && (obj is ConnectivityNode) == false)
                {
                    result = (obj as IdentifiedObject).name;
                }
                // Если объект является полюсом
                else if (obj is Terminal)
                {
                    result = "T" + (obj as Terminal).sequenceNumber + " " + (obj as Terminal).ConductingEquipment.name;
                }
                // Если объект является именуемым объектом, но не полюсом и не точкой подключения
                else if (obj is ConnectivityNode)
                {
                    result = "ConnectivityNode : " + obj.Id + " " + obj.Uid;
                }
                // Если объект не является именуемым
                else if ((obj is IdentifiedObject) == false)
                {
                    result = Convert.ToString(obj.Uid);
                }
            }
            return result;
        }

        string ODUSVGetModelingAuthoritySet<T>(T inputObject)
        {
            string result = String.Empty;
            IdentifiedObject idObj = null;

            if (inputObject is PEStageSetpoint)
                idObj = (Terminal)(inputObject as PEStageSetpoint).Terminal;
            else if (inputObject is IdentifiedObject)
                idObj = inputObject as IdentifiedObject;

            if (idObj != null)
            {
                if (idObj.ModelingAuthoritySet != null)
                    result = idObj.ModelingAuthoritySet.name;
                else
                    result = ODUSVGetModelingAuthoritySet(idObj.ParentObject);
            }

            return result;
        }
    }
}
